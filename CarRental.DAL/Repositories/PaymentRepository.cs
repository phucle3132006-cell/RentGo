using CarRental.DAL.Context;
using CarRental.DAL.Interfaces;
using CarRental.Models.DTOs.Payments;
using CarRental.Models.Entities;
using CarRental.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace CarRental.DAL.Repositories;

public sealed class PaymentRepository(CarRentalDbContext context) : IPaymentRepository
{
    public async Task<(List<RevenueGroup> Groups, int PaidOrders, decimal PreviousRevenue)> ReadRevenueAsync(
        DateTime previousStartUtc, DateTime startUtc, DateTime endUtc, RevenueBucket bucket,
        CancellationToken cancellationToken)
    {
        // One row per recorded payment. SQL enforces unique transaction codes and one PAID row per order.
        // Order status changes do not create or remove receipts. REFUNDED rows are excluded.
        var paid = context.Payments.AsNoTracking()
            .Where(payment => payment.Status == PaymentStatus.PAID && payment.PaidAt.HasValue);
        var current = paid.Where(payment => payment.PaidAt >= startUtc && payment.PaidAt < endUtc);
        // Translate both timestamps to Vietnam time before counting calendar boundaries.
        var localStart = startUtc.AddHours(7);
        var dated = current.Select(payment => new { Payment = payment, Local = payment.PaidAt!.Value.AddHours(7) });
        var grouped = bucket switch
        {
            RevenueBucket.Hour => dated.GroupBy(row => new { Bucket = EF.Functions.DateDiffHour(localStart, row.Local), row.Payment.PaymentMethod }),
            RevenueBucket.Day => dated.GroupBy(row => new { Bucket = EF.Functions.DateDiffDay(localStart, row.Local), row.Payment.PaymentMethod }),
            RevenueBucket.Month => dated.GroupBy(row => new { Bucket = EF.Functions.DateDiffMonth(localStart, row.Local), row.Payment.PaymentMethod }),
            _ => dated.GroupBy(row => new { Bucket = EF.Functions.DateDiffYear(localStart, row.Local), row.Payment.PaymentMethod })
        };
        var groups = await grouped.Select(group => new RevenueGroup(group.Key.Bucket,
            group.Key.PaymentMethod, group.Sum(row => row.Payment.Amount), group.Count()))
            .ToListAsync(cancellationToken);
        var orders = await current.Select(payment => payment.RentalOrderId).Distinct().CountAsync(cancellationToken);
        var previous = await paid.Where(payment => payment.PaidAt >= previousStartUtc && payment.PaidAt < startUtc)
            .SumAsync(payment => payment.Amount, cancellationToken);
        return (groups, orders, previous);
    }

    public Task<Payment?> FindAsync(long orderId, long paymentId, CancellationToken cancellationToken) =>
        context.Payments.AsNoTracking().SingleOrDefaultAsync(
            payment => payment.RentalOrderId == orderId && payment.Id == paymentId, cancellationToken);

    public Task<Payment?> FindByCodeAsync(long orderId, string transactionCode, CancellationToken cancellationToken) =>
        context.Payments.AsNoTracking().SingleOrDefaultAsync(
            payment => payment.RentalOrderId == orderId && payment.TransactionCode == transactionCode,
            cancellationToken);

    public Task<bool> HasActivePaymentAsync(long orderId, CancellationToken cancellationToken) =>
        context.Payments.AnyAsync(payment => payment.RentalOrderId == orderId &&
            (payment.Status == PaymentStatus.PENDING || payment.Status == PaymentStatus.PAID),
            cancellationToken);

    public async Task AddAsync(Payment payment, CancellationToken cancellationToken)
    {
        context.Payments.Add(payment);
        await context.SaveChangesAsync(cancellationToken);
    }
}
