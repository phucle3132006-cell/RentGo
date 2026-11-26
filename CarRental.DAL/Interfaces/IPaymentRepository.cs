using CarRental.Models.Entities;
using CarRental.Models.DTOs.Payments;

namespace CarRental.DAL.Interfaces;

public interface IPaymentRepository
{
    Task<(List<RevenueGroup> Groups, int PaidOrders, decimal PreviousRevenue)> ReadRevenueAsync(
        DateTime previousStartUtc, DateTime startUtc, DateTime endUtc, RevenueBucket bucket,
        CancellationToken cancellationToken);

    Task<Payment?> FindAsync(long orderId, long paymentId, CancellationToken cancellationToken);
    Task<Payment?> FindByCodeAsync(long orderId, string transactionCode, CancellationToken cancellationToken);
    Task<bool> HasActivePaymentAsync(long orderId, CancellationToken cancellationToken);
    Task AddAsync(Payment payment, CancellationToken cancellationToken);
}
