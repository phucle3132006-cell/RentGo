using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Security.Claims;
using CarRental.DAL.Context;
using CarRental.DAL.Interfaces;
using CarRental.Models.Entities;
using CarRental.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;

namespace CarRental.API.Controllers;

[ApiController]
public sealed class TransferRequestsController(CarRentalDbContext context, IRentalOrderRepository orders) : ControllerBase
{
    private const string CustomerPath = "api/rental-orders/{orderId:long}/transfer-requests";
    private const string AdminPath = "api/admin/payments/transfer-requests";

    private IQueryable<RentalOrder> OwnedOrder(long orderId, long actorId) =>
        context.RentalOrders.Where(order => order.Id == orderId && order.CustomerId == actorId);

    private static IQueryable<RentalOrder> WithPaymentState(IQueryable<RentalOrder> query) =>
        query.Include(order => order.RentalOrderDetails)
            .Include(order => order.Payments.Where(payment => payment.Status == PaymentStatus.PAID));

    private IQueryable<TransferRequest> Requests(long orderId) =>
        context.Set<TransferRequest>().Where(item => item.OrderId == orderId);

    private async Task<long?> ActiveActor(UserRole role, CancellationToken ct)
    {
        if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id))
            return null;

        return await context.Users.AnyAsync(user => user.Id == id && user.Role == role &&
            user.Status == UserStatus.ACTIVE, ct) ? id : null;
    }

    private static object View(TransferRequest item) => new
    {
        item.Id, item.PaymentMethod, item.Status, item.SubmittedAt, item.ReviewedAt, item.Reason
    };

    [HttpGet(CustomerPath + "/instructions")]
    [Authorize(Roles = "CUSTOMER")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Instructions(long orderId, CancellationToken ct)
    {
        var actor = await ActiveActor(UserRole.CUSTOMER, ct);
        if (actor is null) return Forbid();
        var order = await WithPaymentState(OwnedOrder(orderId, actor.Value)).AsNoTracking().SingleOrDefaultAsync(ct);
        if (order is null) return NotFound();
        if (!order.CanUseManualPayment() || decimal.Truncate(order.TotalAmount) != order.TotalAmount)
            return Conflict();

        const string bank = "MB Bank";
        const string account = "0879542925";
        const string holder = "LE QUANG PHUC";
        var amount = order.TotalAmount.ToString("0", CultureInfo.InvariantCulture);
        var reference = new string(order.OrderCode.Where(char.IsAsciiLetterOrDigit).ToArray());
        if (reference.Length == 0) return Conflict();

        var beneficiary = Tlv("00", "970422") + Tlv("01", account);
        var merchant = Tlv("00", "A000000727") + Tlv("01", beneficiary) + Tlv("02", "QRIBFTTA");
        var payload = "000201010212" + Tlv("38", merchant) + "5303704" + Tlv("54", amount) +
            "5802VN" + Tlv("62", Tlv("08", reference)) + "6304";
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(payload + Checksum(payload), QRCodeGenerator.ECCLevel.M);
        using var image = new PngByteQRCode(data);
        var png = image.GetGraphic(8);
        return Ok(new { bank, account, holder, amount, reference, qrPng = Convert.ToBase64String(png) });
    }

    private static string Tlv(string tag, string value) =>
        tag + value.Length.ToString("D2", CultureInfo.InvariantCulture) + value;

    private static string Checksum(string payload)
    {
        // CRC-16/CCITT-FALSE, including the checksum tag and length.
        ushort crc = 0xFFFF;
        foreach (var character in payload)
        {
            crc ^= (ushort)(character << 8);
            for (var bit = 0; bit < 8; bit++)
                crc = (ushort)((crc << 1) ^ ((crc & 0x8000) != 0 ? 0x1021 : 0));
        }
        return crc.ToString("X4", CultureInfo.InvariantCulture);
    }

    [HttpGet(CustomerPath)]
    [Authorize(Roles = "CUSTOMER")]
    public async Task<IActionResult> Latest(long orderId, CancellationToken ct)
    {
        var actor = await ActiveActor(UserRole.CUSTOMER, ct);
        if (actor is null) return Forbid();
        if (!await OwnedOrder(orderId, actor.Value).AnyAsync(ct)) return NotFound();

        var latest = await Requests(orderId).OrderByDescending(item => item.Id).FirstOrDefaultAsync(ct);
        return Ok(new { latest = latest is null ? null : View(latest) });
    }

    public sealed record Submission(string? PaymentMethod);

    [HttpPost(CustomerPath)]
    [Authorize(Roles = "CUSTOMER")]
    public async Task<IActionResult> Submit(long orderId, [FromHeader(Name = "Idempotency-Key")] Guid key,
        [FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] Submission? input,
        CancellationToken ct)
    {
        if (key == Guid.Empty) return BadRequest(new { message = "Cần Idempotency-Key hợp lệ." });
        var actor = await ActiveActor(UserRole.CUSTOMER, ct);
        if (actor is null) return Forbid();

        var method = input?.PaymentMethod ?? "BANK_TRANSFER";
        if (method is not ("CASH" or "BANK_TRANSFER"))
            return BadRequest(new { message = "Phương thức thanh toán không hợp lệ." });

        var query = OwnedOrder(orderId, actor.Value);
        var vehicles = await VehicleIds(query, ct);
        if (vehicles is null) return NotFound();
        if (vehicles.Length == 0) return Conflict();

        await using var transaction = await orders.BeginBookingTransactionAsync(vehicles, ct);
        var order = await WithPaymentState(query).AsNoTracking().SingleOrDefaultAsync(ct);
        if (order is null) return NotFound();
        if (!vehicles.Order().SequenceEqual(order.RentalOrderDetails.Select(detail => detail.VehicleId).Order()))
            return Conflict(new { message = "Danh sách xe đã thay đổi. Vui lòng tải lại đơn." });

        var existing = await Requests(orderId).SingleOrDefaultAsync(item => item.SubmissionKey == key, ct);

        if (existing is not null)
            return existing.PaymentMethod == method ? Ok(View(existing)) :
                Conflict(new { message = "Yêu cầu này đã dùng phương thức thanh toán khác." });
        if (await Requests(orderId).AnyAsync(item => item.Status == "PENDING", ct))
            return Conflict(new { message = "Đơn đã có yêu cầu đang chờ xác nhận." });

        if (!order.CanUseManualPayment() ||
            (method == "BANK_TRANSFER" && decimal.Truncate(order.TotalAmount) != order.TotalAmount))
            return Conflict(new { message = "Đơn này không nhận yêu cầu xác nhận thanh toán." });

        var item = new TransferRequest
        {
            OrderId = orderId, CustomerId = actor.Value, SubmissionKey = key,
            PaymentMethod = method, SubmittedAt = DateTime.UtcNow
        };
        context.Add(item);
        await context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Ok(View(item));
    }

    [HttpGet(AdminPath + "/pending-count")]
    [Authorize(Roles = "ADMIN")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> PendingCount(CancellationToken ct)
    {
        if (await ActiveActor(UserRole.ADMIN, ct) is null) return Forbid();
        var pendingCount = await context.Set<TransferRequest>().CountAsync(item => item.Status == "PENDING", ct);
        return Ok(new { pendingCount });
    }

    [HttpGet(AdminPath)]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> List(CancellationToken ct,
        [FromQuery, Range(1, 100000)] int page = 1, [FromQuery, StringLength(100)] string? search = null)
    {
        if (await ActiveActor(UserRole.ADMIN, ct) is null) return Forbid();
        const int pageSize = 20;
        var query = context.Set<TransferRequest>().AsNoTracking();
        var term = search?.Trim();
        if (!string.IsNullOrEmpty(term))
            query = query.Where(item => item.Order.OrderCode.Contains(term) ||
                item.Order.Customer.FullName.Contains(term) || item.Order.Customer.Email.Contains(term) ||
                item.Order.Customer.Phone.Contains(term));

        var totalCount = await query.CountAsync(ct);
        var items = await query.OrderByDescending(item => item.Status == "PENDING")
            .ThenByDescending(item => item.Id).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(item => new
            {
                item.Id, item.OrderId, item.Order.OrderCode, item.Order.TotalAmount,
                CustomerName = item.Order.Customer.FullName, CustomerEmail = item.Order.Customer.Email,
                CustomerPhone = item.Order.Customer.Phone, item.PaymentMethod, OrderStatus = item.Order.Status.ToString(),
                item.Status, item.SubmittedAt, item.ReviewedAt, item.ReviewedByUserId, item.Reason, item.PaymentId
            }).ToListAsync(ct);
        return Ok(new { items, totalCount, pageSize });
    }

    public sealed record Rejection([Required, StringLength(500)] string Reason);

    [HttpPost(AdminPath + "/{id:long}/reject")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Reject(long id, Rejection input, CancellationToken ct)
    {
        var actor = await ActiveActor(UserRole.ADMIN, ct);
        if (actor is null) return Forbid();
        var reason = input.Reason.Trim();
        if (reason.Length == 0) return BadRequest(new { message = "Nhập lý do từ chối." });

        var requestQuery = context.Set<TransferRequest>().Where(item => item.Id == id);
        var vehicles = await VehicleIds(context.RentalOrders.Where(order =>
            requestQuery.Any(item => item.OrderId == order.Id)), ct);
        if (vehicles is null) return NotFound();
        if (vehicles.Length == 0) return Conflict();

        await using var transaction = await orders.BeginBookingTransactionAsync(vehicles, ct);
        var item = await requestQuery.SingleAsync(ct);
        if (item.Status == "REJECTED" && item.ReviewedByUserId == actor && item.Reason == reason)
            return Ok(View(item));
        if (item.Status != "PENDING") return Conflict(new { message = "Yêu cầu đã được xử lý." });

        item.Status = "REJECTED";
        item.Reason = reason;
        item.ReviewedAt = DateTime.UtcNow;
        item.ReviewedByUserId = actor;
        await context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Ok(View(item));
    }

    private static async Task<long[]?> VehicleIds(IQueryable<RentalOrder> query, CancellationToken ct) =>
        await query.Select(order => order.RentalOrderDetails.Select(detail => detail.VehicleId).ToArray())
            .SingleOrDefaultAsync(ct);
}
