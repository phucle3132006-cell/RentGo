using CarRental.Models.Entities;

namespace CarRental.DAL.Interfaces;

public interface IPaymentRepository
{
    Task<Payment?> FindAsync(long orderId, long paymentId, CancellationToken cancellationToken);
    Task<Payment?> FindByCodeAsync(long orderId, string transactionCode, CancellationToken cancellationToken);
    Task<bool> HasActivePaymentAsync(long orderId, CancellationToken cancellationToken);
    Task AddAsync(Payment payment, CancellationToken cancellationToken);
}
