using CarRental.Models.DTOs.RentalOrders;

namespace CarRental.BLL.Interfaces
{
    public interface IRentalOrderService
    {
        // customerId must come from the authenticated caller.
        Task<RentalOrderResponse> CreateAsync(
            long customerId,
            CreateRentalOrderRequest request,
            CancellationToken cancellationToken = default);

        // A null customerId allows an administrator to look up any order.
        Task<RentalOrderResponse?> GetByIdAsync(
            long orderId,
            long? customerId,
            CancellationToken cancellationToken = default);

        Task<RentalOrderListResponse> ListByCustomerAsync(
            long customerId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);

        // actorId must come from the authenticated caller.
        Task<RentalOrderResponse> UpdateStatusAsync(
            long orderId,
            long actorId,
            UpdateRentalOrderStatusRequest request,
            CancellationToken cancellationToken = default);
    }
}
