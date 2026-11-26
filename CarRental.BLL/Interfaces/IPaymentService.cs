using CarRental.Models.DTOs.Payments;
using CarRental.Models.Enums;

namespace CarRental.BLL.Interfaces;

public interface IPaymentService
{
    Task<RevenueReport> GetRevenueAsync(RevenueQuery query, CancellationToken cancellationToken);

    Task<(PaymentResponse Payment, bool Created)> CreateAsync(
        long orderId, long customerId, Guid key, CreatePaymentRequest request, CancellationToken cancellationToken);

    Task<(PaymentResponse Payment, bool Created)> ConfirmManualAsync(
        long orderId, long adminId, Guid key, ConfirmManualPaymentRequest request, CancellationToken cancellationToken);

    Task<PaymentResponse> GetAsync(
        long orderId, long paymentId, long actorId, UserRole role, CancellationToken cancellationToken);
}
