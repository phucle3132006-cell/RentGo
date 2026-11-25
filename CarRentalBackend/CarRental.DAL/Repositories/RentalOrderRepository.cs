using System.Data;
using CarRental.DAL.Context;
using CarRental.DAL.Interfaces;
using CarRental.Models.Entities;
using CarRental.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace CarRental.DAL.Repositories
{
    public class RentalOrderRepository : IRentalOrderRepository
    {
        private readonly CarRentalDbContext _context;
        private Guid? _bookingTransactionId;
        private HashSet<long> _lockedVehicleIds = new();

        public RentalOrderRepository(CarRentalDbContext context)
        {
            _context = context;
        }

        public Task<RentalOrder?> GetByIdAsync(
            long id, CancellationToken cancellationToken = default)
        {
            return _context.RentalOrders
                .Include(order => order.RentalOrderDetails)
                .Include(order => order.Payments.Where(payment => payment.Status == PaymentStatus.PAID))
                .FirstOrDefaultAsync(order => order.Id == id, cancellationToken);
        }

        public Task<List<RentalOrder>> ListByCustomerAsync(
            long customerId, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(page);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);
            var offset = checked((page - 1) * pageSize);

            return _context.RentalOrders
                .AsNoTracking()
                .Where(order => order.CustomerId == customerId)
                .Include(order => order.RentalOrderDetails)
                .Include(order => order.Payments.Where(payment => payment.Status == PaymentStatus.PAID))
                .AsSplitQuery()
                .OrderByDescending(order => order.CreatedAt)
                .ThenByDescending(order => order.Id)
                .Skip(offset)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        public async Task<IDbContextTransaction> BeginBookingTransactionAsync(
            IEnumerable<long> vehicleIds, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(vehicleIds);
            var orderedIds = vehicleIds.Distinct().OrderBy(id => id).ToArray();
            if (orderedIds.Length == 0)
            {
                throw new ArgumentException("At least one vehicle is required.", nameof(vehicleIds));
            }

            var transaction = await _context.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted, cancellationToken);
            try
            {
                foreach (var vehicleId in orderedIds)
                {
                    // Issue one primary-key lookup at a time to enforce lock order.
                    var rows = await _context.Database.SqlQuery<long>($"""
                        SELECT [id] AS [Value]
                        FROM [dbo].[vehicles] WITH (UPDLOCK, ROWLOCK, INDEX([PK_vehicles]))
                        WHERE [id] = {vehicleId}
                        """).ToListAsync(cancellationToken);

                    if (rows.Count == 0)
                    {
                        throw new KeyNotFoundException($"Vehicle {vehicleId} does not exist.");
                    }
                }

                _lockedVehicleIds = orderedIds.ToHashSet();
                _bookingTransactionId = transaction.TransactionId;
                return transaction;
            }
            catch
            {
                await transaction.DisposeAsync();
                throw;
            }
        }

        public Task<bool> HasOverlappingBookingAsync(
            long vehicleId, DateTime requestedStart, DateTime requestedEnd,
            CancellationToken cancellationToken = default)
        {
            RequireVehicleLock(vehicleId);
            if (requestedStart >= requestedEnd)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(requestedEnd), "The end must be after the start.");
            }

            return OverlappingDetails(_context.RentalOrderDetails, requestedStart, requestedEnd)
                .AnyAsync(detail => detail.VehicleId == vehicleId, cancellationToken);
        }

        internal static IQueryable<RentalOrderDetail> OverlappingDetails(
            IQueryable<RentalOrderDetail> details, DateTime requestedStart, DateTime requestedEnd)
        {
            return details.Where(detail =>
                (detail.Status == RentalDetailStatus.RESERVED ||
                 detail.Status == RentalDetailStatus.RENTING) &&
                (detail.RentalOrder.Status == RentalStatus.PENDING ||
                 detail.RentalOrder.Status == RentalStatus.CONFIRMED ||
                 detail.RentalOrder.Status == RentalStatus.PAID ||
                 detail.RentalOrder.Status == RentalStatus.RENTING) &&
                detail.RentalOrder.StartDatetime < requestedEnd &&
                requestedStart < detail.RentalOrder.EndDatetime);
        }

        public async Task AddAsync(RentalOrder order, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(order);
            if (order.RentalOrderDetails.Count == 0)
            {
                throw new ArgumentException("At least one rental detail is required.", nameof(order));
            }

            foreach (var detail in order.RentalOrderDetails)
            {
                RequireVehicleLock(detail.VehicleId);
            }

            await _context.RentalOrders.AddAsync(order, cancellationToken);
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            RequireBookingTransaction();
            return _context.SaveChangesAsync(cancellationToken);
        }

        private void RequireBookingTransaction()
        {
            var transaction = _context.Database.CurrentTransaction;
            if (transaction is null || transaction.TransactionId != _bookingTransactionId)
            {
                throw new InvalidOperationException("An active booking transaction is required.");
            }
        }

        private void RequireVehicleLock(long vehicleId)
        {
            RequireBookingTransaction();
            if (!_lockedVehicleIds.Contains(vehicleId))
            {
                throw new InvalidOperationException($"Vehicle {vehicleId} is not locked by this transaction.");
            }
        }
    }
}
