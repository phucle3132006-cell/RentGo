using CarRental.Models.Entities;
using Microsoft.EntityFrameworkCore.Storage;

namespace CarRental.DAL.Interfaces
{
    public interface IRentalOrderRepository
    {
        Task<RentalOrder?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

        // Page numbers start at 1.
        Task<List<RentalOrder>> ListByCustomerAsync(
            long customerId, int page, int pageSize, CancellationToken cancellationToken = default);

        // Pass all affected vehicle IDs together. The caller owns the transaction:
        // dispose it on every exit and commit only after validation and saving succeed.
        // A missing vehicle causes KeyNotFoundException.
        Task<IDbContextTransaction> BeginBookingTransactionAsync(
            IEnumerable<long> vehicleIds, CancellationToken cancellationToken = default);

        // Call inside the booking transaction after locking the requested vehicle.
        Task<bool> HasOverlappingBookingAsync(
            long vehicleId, DateTime requestedStart, DateTime requestedEnd,
            CancellationToken cancellationToken = default);

        // Stage the order and its details after checking each vehicle for conflicts.
        Task AddAsync(RentalOrder order, CancellationToken cancellationToken = default);

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}