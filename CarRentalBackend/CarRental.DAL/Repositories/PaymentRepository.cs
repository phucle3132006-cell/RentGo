using CarRental.DAL.Context;
using CarRental.DAL.Interfaces;
using CarRental.Models.Entities;
using CarRental.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace CarRental.DAL.Repositories;

public sealed class PaymentRepository(CarRentalDbContext context) : IPaymentRepository
{
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
