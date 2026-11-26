using CarRental.BLL.Interfaces;
using CarRental.DAL.Context;
using CarRental.DAL.Interfaces;
using CarRental.Models.DTOs.Payments;
using CarRental.Models.Entities;
using CarRental.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace CarRental.BLL.Services;

public sealed partial class PaymentService(
    CarRentalDbContext context,
    IRentalOrderRepository orders,
    IPaymentRepository payments) : IPaymentService
{
    private const decimal MaxAmount = 9_999_999_999.99m;

    public async Task<(PaymentResponse Payment, bool Created)> CreateAsync(
        long orderId, long customerId, Guid key, CreatePaymentRequest request, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(orderId);
        if (key == Guid.Empty ||
            !Enum.TryParse<PaymentMethod>(request.PaymentMethod, true, out var method) ||
            !Enum.IsDefined(method) ||
            !string.Equals(method.ToString(), request.PaymentMethod, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("A valid idempotency key and payment method are required.");
        }

        await RequireActiveUserAsync(customerId, UserRole.CUSTOMER, cancellationToken);
        var orderQuery = context.RentalOrders.AsNoTracking()
            .Where(order => order.Id == orderId && order.CustomerId == customerId);
        var vehicleIds = await orderQuery.Select(order => order.RentalOrderDetails
                .Select(detail => detail.VehicleId).OrderBy(id => id).ToArray())
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException();
        if (vehicleIds.Length == 0)
            throw new InvalidOperationException("The rental order has no vehicles.");

        // Use the same locks and ordering as rental cancellation.
        await using var transaction = await orders.BeginBookingTransactionAsync(vehicleIds, cancellationToken);
        var order = await orderQuery.Include(item => item.RentalOrderDetails)
            .SingleOrDefaultAsync(cancellationToken) ?? throw new KeyNotFoundException();
        if (!vehicleIds.SequenceEqual(order.RentalOrderDetails.Select(detail => detail.VehicleId).OrderBy(id => id)))
            throw new InvalidOperationException("The order vehicles changed. Retry the request.");

        var transactionCode = FormattableString.Invariant($"RG-{orderId}-{key:N}");
        var existing = await payments.FindByCodeAsync(orderId, transactionCode, cancellationToken);
        if (existing is not null)
        {
            if (existing.PaymentMethod != method)
                throw new InvalidOperationException("The idempotency key is already used with another payment method.");

            await transaction.CommitAsync(cancellationToken);
            return (Map(existing), false);
        }

        if (order.Status is not (RentalStatus.PENDING or RentalStatus.CONFIRMED) ||
            order.RentalOrderDetails.Any(detail => detail.Status != RentalDetailStatus.RESERVED))
        {
            throw new InvalidOperationException("The rental order is unavailable for payment.");
        }

        if (order.TotalAmount <= 0 || order.TotalAmount > MaxAmount ||
            decimal.Round(order.TotalAmount, 2) != order.TotalAmount)
        {
            throw new InvalidOperationException("The rental order has an invalid payment amount.");
        }

        if (await payments.HasActivePaymentAsync(orderId, cancellationToken))
            throw new InvalidOperationException("The rental order already has an active payment.");

        var payment = new Payment
        {
            RentalOrderId = orderId,
            Amount = order.TotalAmount,
            PaymentMethod = method,
            TransactionCode = transactionCode,
            Status = PaymentStatus.PENDING,
            CreatedAt = DateTime.UtcNow
        };
        await payments.AddAsync(payment, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return (Map(payment), true);
    }

    public async Task<(PaymentResponse Payment, bool Created)> ConfirmManualAsync(
        long orderId, long adminId, Guid key, ConfirmManualPaymentRequest request, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(orderId);
        if (key == Guid.Empty || !request.FundsVerified ||
            request.Amount <= 0 || request.Amount > MaxAmount || decimal.Round(request.Amount, 2) != request.Amount)
        {
            throw new ArgumentException("Verify the received funds and provide a valid amount.");
        }

        var method = request.PaymentMethod switch
        {
            "CASH" => PaymentMethod.CASH,
            "BANK_TRANSFER" => PaymentMethod.BANK_TRANSFER,
            _ => throw new ArgumentException("Use CASH or BANK_TRANSFER.")
        };
        var reference = request.ExternalReference?.Trim();
        if (reference?.Length > 100)
            throw new ArgumentException("The payment reference must be at most 100 characters.");

        reference = string.IsNullOrEmpty(reference) ? null : reference.ToUpperInvariant();
        await RequireActiveUserAsync(adminId, UserRole.ADMIN, cancellationToken);

        var orderQuery = context.RentalOrders.Where(order => order.Id == orderId);
        var vehicleIds = await orderQuery.Select(order => order.RentalOrderDetails
                .Select(detail => detail.VehicleId).OrderBy(id => id).ToArray())
            .SingleOrDefaultAsync(cancellationToken) ?? throw new KeyNotFoundException();
        if (vehicleIds.Length == 0)
            throw new InvalidOperationException("The rental order has no vehicles.");

        await using var transaction = await orders.BeginBookingTransactionAsync(vehicleIds, cancellationToken);
        await RequireActiveUserAsync(adminId, UserRole.ADMIN, cancellationToken);
        var order = await orderQuery.Include(item => item.RentalOrderDetails)
            .SingleOrDefaultAsync(cancellationToken) ?? throw new KeyNotFoundException();
        if (!vehicleIds.SequenceEqual(order.RentalOrderDetails.Select(detail => detail.VehicleId).OrderBy(id => id)))
            throw new InvalidOperationException("The order vehicles changed. Retry the request.");

        var code = FormattableString.Invariant($"RG-M-{orderId}-{key:N}");
        var existing = await payments.FindByCodeAsync(orderId, code, cancellationToken);
        if (existing is not null)
        {
            if (existing.ConfirmedByUserId != adminId || existing.PaymentMethod != method ||
                existing.Amount != request.Amount || existing.ExternalReference != reference ||
                (request.TransferRequestId.HasValue && !await context.Set<TransferRequest>().AnyAsync(
                    item => item.Id == request.TransferRequestId && item.PaymentId == existing.Id, cancellationToken)))
            {
                throw new InvalidOperationException("The idempotency key is already used with different confirmation details.");
            }

            await transaction.CommitAsync(cancellationToken);
            return (Map(existing), false);
        }

        if (order.Status is not (RentalStatus.PENDING or RentalStatus.CONFIRMED) ||
            order.RentalOrderDetails.Any(detail => detail.Status != RentalDetailStatus.RESERVED))
            throw new InvalidOperationException("Only an unpaid rental order with reserved vehicles can be paid.");

        if (request.Amount != order.TotalAmount)
            throw new ArgumentException("The received amount must match the order total.");

        var active = await context.Payments.Where(payment => payment.RentalOrderId == orderId &&
            (payment.Status == PaymentStatus.PENDING || payment.Status == PaymentStatus.PAID))
            .ToListAsync(cancellationToken);
        if (active.Any(payment => payment.Status == PaymentStatus.PAID))
            throw new InvalidOperationException("The rental order already has a paid payment.");

        var transfer = await context.Set<TransferRequest>().SingleOrDefaultAsync(
            item => item.OrderId == orderId && item.Status == "PENDING", cancellationToken);
        // A manual confirmation must correspond to a request submitted by the customer.
        // Do this after the idempotency check so already successful retries still work.
        if (transfer is null)
            throw new InvalidOperationException("Khách hàng chưa gửi yêu cầu xác nhận thanh toán.");
        if (request.TransferRequestId.HasValue && transfer.Id != request.TransferRequestId.Value)
            throw new InvalidOperationException("Yêu cầu xác nhận đã được xử lý hoặc không hợp lệ.");
        if (transfer.PaymentMethod != method.ToString())
            throw new InvalidOperationException("Phương thức phải khớp với yêu cầu xác nhận đang chờ.");

        // Customer-created rows are unpaid intents. Close them once funds are manually verified.
        foreach (var pending in active)
            pending.Status = PaymentStatus.FAILED;

        var now = DateTime.UtcNow;
        var payment = new Payment
        {
            RentalOrderId = orderId,
            Amount = order.TotalAmount,
            PaymentMethod = method,
            TransactionCode = code,
            Status = PaymentStatus.PAID,
            ConfirmedByUserId = adminId,
            ExternalReference = reference,
            PaidAt = now,
            CreatedAt = now
        };
        order.Status = RentalStatus.PAID;
        order.UpdatedAt = now;

        try
        {
            await payments.AddAsync(payment, cancellationToken);
            if (transfer is not null)
            {
                transfer.Status = "APPROVED";
                transfer.ReviewedAt = now;
                transfer.ReviewedByUserId = adminId;
                transfer.PaymentId = payment.Id;
                await context.SaveChangesAsync(cancellationToken);
            }
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException error) when (error.InnerException is SqlException sql &&
            sql.Number == 2601 && (sql.Message.Contains("UX_payments_external_reference", StringComparison.Ordinal) ||
                sql.Message.Contains("UX_payments_paid_order", StringComparison.Ordinal)))
        {
            throw new InvalidOperationException("The order was already paid or this payment reference has already been used.", error);
        }

        return (Map(payment), true);
    }

    public async Task<PaymentResponse> GetAsync(
        long orderId, long paymentId, long actorId, UserRole role, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(orderId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(paymentId);
        await RequireActiveUserAsync(actorId, role, cancellationToken);

        if (role == UserRole.CUSTOMER && !await context.RentalOrders.AnyAsync(
                order => order.Id == orderId && order.CustomerId == actorId, cancellationToken))
        {
            throw new KeyNotFoundException();
        }

        var payment = await payments.FindAsync(orderId, paymentId, cancellationToken)
            ?? throw new KeyNotFoundException();
        return Map(payment);
    }

    private async Task RequireActiveUserAsync(long actorId, UserRole role, CancellationToken cancellationToken)
    {
        if (role is not (UserRole.CUSTOMER or UserRole.ADMIN) || !await context.Users.AnyAsync(
                user => user.Id == actorId && user.Role == role && user.Status == UserStatus.ACTIVE,
                cancellationToken))
        {
            throw new UnauthorizedAccessException("An active account is required.");
        }
    }

    private static PaymentResponse Map(Payment payment) =>
        new(payment.Id, payment.RentalOrderId, payment.Amount, payment.PaymentMethod.ToString(),
            payment.TransactionCode, payment.Status.ToString(), payment.PaidAt, payment.CreatedAt);
}
