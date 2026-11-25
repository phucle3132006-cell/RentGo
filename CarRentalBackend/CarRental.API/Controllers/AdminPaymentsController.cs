using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using CarRental.DAL.Context;
using CarRental.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarRental.API.Controllers;

[ApiController]
[Authorize(Roles = "ADMIN")]
[Route("api/admin/payments")]
public sealed class AdminPaymentsController(CarRentalDbContext context) : ControllerBase
{
    private async Task<bool> IsActiveAdmin(CancellationToken cancellationToken) =>
        long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) &&
        await context.Users.AnyAsync(user => user.Id == id && user.Role == UserRole.ADMIN &&
            user.Status == UserStatus.ACTIVE, cancellationToken);

    [HttpGet("summary")]
    public async Task<IActionResult> Summary(CancellationToken cancellationToken)
    {
        if (!await IsActiveAdmin(cancellationToken)) return Forbid();

        var paid = await context.Payments.Where(payment => payment.Status == PaymentStatus.PAID)
            .GroupBy(_ => 1)
            .Select(group => new { Revenue = group.Sum(payment => payment.Amount), Count = group.Count() })
            .SingleOrDefaultAsync(cancellationToken);
        var counts = await context.RentalOrders
            .Where(order => order.Status == RentalStatus.PENDING || order.Status == RentalStatus.CONFIRMED)
            .GroupBy(order => order.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.Status, item => item.Count, cancellationToken);

        return Ok(new
        {
            revenue = paid?.Revenue ?? 0m,
            paidOrders = paid?.Count ?? 0,
            awaitingReview = counts.GetValueOrDefault(RentalStatus.PENDING),
            awaitingPayment = counts.GetValueOrDefault(RentalStatus.CONFIRMED)
        });
    }

    [HttpGet]
    public async Task<IActionResult> List(
        CancellationToken cancellationToken,
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 20,
        [FromQuery, StringLength(100)] string? search = null,
        [FromQuery] PaymentStatus? status = null,
        [FromQuery] PaymentMethod? method = null,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? before = null)
    {
        if (!await IsActiveAdmin(cancellationToken)) return Forbid();

        var offset = (page - 1L) * pageSize;
        if (offset > int.MaxValue || (status.HasValue && !Enum.IsDefined(status.Value)) ||
            (method.HasValue && !Enum.IsDefined(method.Value)) || from >= before)
            return BadRequest(new { message = "Trang hoặc bộ lọc giao dịch không hợp lệ." });

        var query = context.Payments.AsNoTracking();
        if (status.HasValue) query = query.Where(payment => payment.Status == status.Value);
        if (method.HasValue) query = query.Where(payment => payment.PaymentMethod == method.Value);
        if (from.HasValue)
        {
            var start = from.Value.UtcDateTime;
            query = query.Where(payment => (payment.PaidAt ?? payment.CreatedAt) >= start);
        }
        if (before.HasValue)
        {
            var end = before.Value.UtcDateTime;
            query = query.Where(payment => (payment.PaidAt ?? payment.CreatedAt) < end);
        }

        var term = search?.Trim();
        if (!string.IsNullOrEmpty(term))
        {
            long.TryParse(term, out var orderId);
            query = query.Where(payment => payment.RentalOrderId == orderId ||
                payment.TransactionCode.Contains(term) ||
                (payment.ExternalReference != null && payment.ExternalReference.Contains(term)) ||
                payment.RentalOrder.OrderCode.Contains(term) ||
                payment.RentalOrder.Customer.FullName.Contains(term) ||
                payment.RentalOrder.Customer.Email.Contains(term) ||
                payment.RentalOrder.Customer.Phone.Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(payment => payment.PaidAt ?? payment.CreatedAt)
            .ThenByDescending(payment => payment.Id)
            .Skip((int)offset).Take(pageSize)
            .Select(payment => new
            {
                payment.Id,
                payment.RentalOrderId,
                payment.RentalOrder.OrderCode,
                OrderStatus = payment.RentalOrder.Status,
                payment.RentalOrder.CustomerId,
                CustomerName = payment.RentalOrder.Customer.FullName,
                CustomerEmail = payment.RentalOrder.Customer.Email,
                CustomerPhone = payment.RentalOrder.Customer.Phone,
                payment.Amount,
                payment.Status,
                payment.PaymentMethod,
                payment.TransactionCode,
                payment.ExternalReference,
                payment.PaidAt,
                payment.CreatedAt,
                payment.ConfirmedByUserId,
                ConfirmedByName = context.Users.Where(user => user.Id == payment.ConfirmedByUserId)
                    .Select(user => user.FullName).FirstOrDefault()
            }).ToListAsync(cancellationToken);

        return Ok(new
        {
            items = items.Select(item => new
            {
                item.Id, item.RentalOrderId, item.OrderCode, OrderStatus = item.OrderStatus.ToString(),
                item.CustomerId, item.CustomerName, item.CustomerEmail, item.CustomerPhone, item.Amount,
                Status = item.Status.ToString(), PaymentMethod = item.PaymentMethod.ToString(),
                item.TransactionCode, item.ExternalReference, item.PaidAt, item.CreatedAt,
                item.ConfirmedByUserId, item.ConfirmedByName
            }),
            totalCount, page, pageSize
        });
    }
}
