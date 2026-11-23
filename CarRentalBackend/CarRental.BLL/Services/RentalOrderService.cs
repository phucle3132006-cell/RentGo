using System.Security.Cryptography;
using CarRental.BLL.Interfaces;
using CarRental.DAL.Context;
using CarRental.DAL.Interfaces;
using CarRental.Models.DTOs.RentalOrders;
using CarRental.Models.Entities;
using CarRental.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace CarRental.BLL.Services
{
    public class RentalOrderService : IRentalOrderService
    {
        private const decimal MaxAmount = 99_999_999.99m;
        private readonly IRentalOrderRepository _rentalOrderRepository;
        private readonly CarRentalDbContext _context;

        public RentalOrderService(
            IRentalOrderRepository rentalOrderRepository,
            CarRentalDbContext context)
        {
            _rentalOrderRepository = rentalOrderRepository;
            _context = context;
        }

        public async Task<RentalOrderResponse> CreateAsync(
            long customerId,
            CreateRentalOrderRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var vehicleIds = request.VehicleIds?.OrderBy(id => id).ToArray();
            if (vehicleIds is null || vehicleIds.Length == 0 ||
                vehicleIds.Any(id => id <= 0) ||
                vehicleIds.Distinct().Count() != vehicleIds.Length)
            {
                throw new ArgumentException(
                    "Vehicle IDs must be positive and distinct, and at least one is required.",
                    nameof(request));
            }

            if (request.StartDatetime is not DateTimeOffset start ||
                request.EndDatetime is not DateTimeOffset end || end <= start)
            {
                throw new ArgumentException(
                    "Both rental times are required and the end must be after the start.",
                    nameof(request));
            }

            var startUtc = start.UtcDateTime;
            var endUtc = end.UtcDateTime;
            EnsureStartIsNotInPast(startUtc);
            var rentalDays = (int)Math.Ceiling(
                (decimal)(endUtc - startUtc).Ticks / TimeSpan.TicksPerDay);

            if (customerId <= 0 || !await _context.Users.AnyAsync(
                user => user.Id == customerId &&
                    user.Role == UserRole.CUSTOMER && user.Status == UserStatus.ACTIVE,
                cancellationToken))
            {
                throw new UnauthorizedAccessException("An active customer account is required.");
            }

            // The repository and this service share the same scoped DbContext.
            await using var transaction = await _rentalOrderRepository
                .BeginBookingTransactionAsync(vehicleIds, cancellationToken);

            var vehicles = await _context.Vehicles
                .Where(vehicle => vehicleIds.Contains(vehicle.Id))
                .Select(vehicle => new
                {
                    vehicle.Id,
                    vehicle.Status,
                    vehicle.VehicleModel.PricePerDay
                })
                .ToDictionaryAsync(vehicle => vehicle.Id, cancellationToken);

            var now = DateTime.UtcNow;
            var order = new RentalOrder
            {
                OrderCode = Convert.ToHexString(RandomNumberGenerator.GetBytes(15)),
                CustomerId = customerId,
                StartDatetime = startUtc,
                EndDatetime = endUtc,
                Status = RentalStatus.PENDING,
                Note = request.Note,
                CreatedAt = now,
                UpdatedAt = now
            };

            foreach (var vehicleId in vehicleIds)
            {
                var vehicle = vehicles[vehicleId];
                if (vehicle.Status == VehicleStatus.MAINTENANCE ||
                    await _rentalOrderRepository.HasOverlappingBookingAsync(
                        vehicleId, startUtc, endUtc, cancellationToken))
                {
                    throw new InvalidOperationException(
                        $"Vehicle {vehicleId} is unavailable for the requested period.");
                }

                var pricePerDay = vehicle.PricePerDay;
                if (pricePerDay <= 0 || pricePerDay > MaxAmount ||
                    decimal.Round(pricePerDay, 2) != pricePerDay)
                {
                    throw new InvalidOperationException($"Vehicle {vehicleId} has an invalid daily rate.");
                }

                var subtotal = pricePerDay * rentalDays;
                var total = order.TotalAmount + subtotal;
                if (subtotal > MaxAmount || total > MaxAmount)
                {
                    throw new ArgumentException(
                        "The rental amount exceeds the supported decimal(10,2) range.",
                        nameof(request));
                }

                order.RentalOrderDetails.Add(new RentalOrderDetail
                {
                    VehicleId = vehicleId,
                    PricePerDay = pricePerDay,
                    RentalDays = rentalDays,
                    Subtotal = subtotal,
                    Status = RentalDetailStatus.RESERVED,
                    CreatedAt = now
                });
                order.TotalAmount = total;
            }

            EnsureStartIsNotInPast(startUtc);
            try
            {
                await _rentalOrderRepository.AddAsync(order, cancellationToken);
                await _rentalOrderRepository.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                foreach (var detail in order.RentalOrderDetails.ToArray())
                {
                    _context.Entry(detail).State = EntityState.Detached;
                }

                _context.Entry(order).State = EntityState.Detached;
                throw;
            }

            return MapToResponse(order);
        }

        public async Task<RentalOrderResponse?> GetByIdAsync(
            long orderId,
            long? customerId,
            CancellationToken cancellationToken = default)
        {
            var order = await _rentalOrderRepository.GetByIdAsync(orderId, cancellationToken);
            if (order == null || (customerId.HasValue && order.CustomerId != customerId.Value))
            {
                return null;
            }

            return MapToResponse(order);
        }

        public async Task<RentalOrderListResponse> ListByCustomerAsync(
            long customerId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(page);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);
            var offset = (page - 1L) * pageSize;
            if (offset > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(page), "The page offset is too large.");
            }

            var orders = await _rentalOrderRepository.ListByCustomerAsync(customerId, page, pageSize, cancellationToken);
            var totalCount = await _context.RentalOrders.CountAsync(order => order.CustomerId == customerId, cancellationToken);

            return new RentalOrderListResponse
            {
                Items = orders.Select(MapToResponse).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        public async Task<RentalOrderResponse> UpdateStatusAsync(
            long orderId,
            long actorId,
            UpdateRentalOrderStatusRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(orderId);
            ArgumentNullException.ThrowIfNull(request);
            if (request.Status is not RentalStatus target || !Enum.IsDefined(target))
            {
                throw new ArgumentException("A valid rental status is required.", nameof(request));
            }

            var role = await _context.Users
                .Where(user => user.Id == actorId && user.Status == UserStatus.ACTIVE)
                .Select(user => (UserRole?)user.Role)
                .SingleOrDefaultAsync(cancellationToken);
            if (role is not (UserRole.ADMIN or UserRole.CUSTOMER))
            {
                throw new UnauthorizedAccessException("An active account is required.");
            }

            if (request.IsReview)
            {
                if (role != UserRole.ADMIN)
                    throw new UnauthorizedAccessException("Only an administrator can review orders.");

                if ((target == RentalStatus.CANCELLED && (string.IsNullOrWhiteSpace(request.RejectionReason) ||
                        request.RejectionReason.Length > 500)) ||
                    (target == RentalStatus.CONFIRMED && request.RejectionReason != null) ||
                    target is not (RentalStatus.CANCELLED or RentalStatus.CONFIRMED))
                    throw new ArgumentException("A rejection requires a reason of 1–500 characters.");
            }
            else if (target is not (RentalStatus.CANCELLED or RentalStatus.RENTING or RentalStatus.COMPLETED))
            {
                throw new ArgumentException(
                    "This operation only accepts CANCELLED, RENTING, or COMPLETED.", nameof(request));
            }

            if (role == UserRole.CUSTOMER && target != RentalStatus.CANCELLED)
            {
                throw new UnauthorizedAccessException("Only an administrator can record handover or return.");
            }

            var orderQuery = _context.RentalOrders.Where(order => order.Id == orderId);
            if (role == UserRole.CUSTOMER)
            {
                orderQuery = orderQuery.Where(order => order.CustomerId == actorId);
            }

            var booking = await orderQuery
                .Select(order => new
                {
                    VehicleIds = order.RentalOrderDetails
                        .Select(detail => detail.VehicleId)
                        .OrderBy(id => id)
                        .ToArray()
                })
                .SingleOrDefaultAsync(cancellationToken)
                ?? throw new KeyNotFoundException($"Rental order {orderId} does not exist.");

            if (booking.VehicleIds.Length == 0)
            {
                throw new InvalidOperationException("The rental order has no vehicles.");
            }

            await using var transaction = await _rentalOrderRepository
                .BeginBookingTransactionAsync(booking.VehicleIds, cancellationToken);

            // Read current state after acquiring the same locks used by CreateAsync.
            var order = await orderQuery.AsNoTracking()
                .Include(item => item.RentalOrderDetails)
                .SingleOrDefaultAsync(cancellationToken)
                ?? throw new KeyNotFoundException($"Rental order {orderId} does not exist.");

            if (!booking.VehicleIds.SequenceEqual(
                order.RentalOrderDetails.Select(detail => detail.VehicleId).OrderBy(id => id)))
            {
                throw new InvalidOperationException("The order vehicles changed. Retry the status update.");
            }

            if (request.IsReview && order.Status != RentalStatus.PENDING)
                throw new InvalidOperationException("Only orders awaiting review can be approved or rejected.");

            var (expectedDetailStatus, nextDetailStatus) = (order.Status, target) switch
            {
                (RentalStatus.PENDING, RentalStatus.CONFIRMED) =>
                    (RentalDetailStatus.RESERVED, RentalDetailStatus.RESERVED),
                (RentalStatus.PENDING or RentalStatus.CONFIRMED, RentalStatus.CANCELLED) =>
                    (RentalDetailStatus.RESERVED, RentalDetailStatus.CANCELLED),
                (RentalStatus.PAID, RentalStatus.RENTING) =>
                    (RentalDetailStatus.RESERVED, RentalDetailStatus.RENTING),
                (RentalStatus.RENTING, RentalStatus.COMPLETED) =>
                    (RentalDetailStatus.RENTING, RentalDetailStatus.RETURNED),
                _ => throw new InvalidOperationException(
                    $"Cannot change rental status from {order.Status} to {target}.")
            };

            if (order.RentalOrderDetails.Any(detail => detail.Status != expectedDetailStatus))
            {
                throw new InvalidOperationException("Vehicle detail statuses do not match the order status.");
            }

            if (target == RentalStatus.CANCELLED && await _context.Payments.AnyAsync(
                payment => payment.RentalOrderId == orderId && payment.Status == PaymentStatus.PAID,
                cancellationToken))
            {
                throw new InvalidOperationException("The order has a paid transaction and requires payment review.");
            }

            if (target == RentalStatus.RENTING && await _context.RentalOrderDetails.AnyAsync(
                detail => detail.RentalOrderId != orderId &&
                    booking.VehicleIds.Contains(detail.VehicleId) &&
                    detail.Status == RentalDetailStatus.RENTING,
                cancellationToken))
            {
                throw new InvalidOperationException("A vehicle is still on rent under another order.");
            }

            var now = DateTime.UtcNow;
            var updatedOrders = await orderQuery.Where(item => item.Status == order.Status)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(item => item.Status, target)
                    .SetProperty(item => item.UpdatedAt, now)
                    .SetProperty(item => item.ReviewedByUserId, item => request.IsReview ? actorId : item.ReviewedByUserId)
                    .SetProperty(item => item.ReviewedAt, item => request.IsReview ? now : item.ReviewedAt)
                    .SetProperty(item => item.RejectionReason, item => request.IsReview ? request.RejectionReason : item.RejectionReason),
                    cancellationToken);
            var updatedDetails = await _context.RentalOrderDetails
                .Where(detail => detail.RentalOrderId == orderId &&
                    booking.VehicleIds.Contains(detail.VehicleId) && detail.Status == expectedDetailStatus)
                .ExecuteUpdateAsync(setters => setters.SetProperty(detail => detail.Status, nextDetailStatus),
                    cancellationToken);
            if (updatedOrders != 1 || updatedDetails != booking.VehicleIds.Length)
            {
                throw new InvalidOperationException("The rental order changed. Retry the status update.");
            }

            if (target == RentalStatus.CANCELLED)
            {
                await _context.Set<TransferRequest>()
                    .Where(item => item.OrderId == orderId && item.Status == "PENDING")
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(item => item.Status, "CANCELLED")
                        .SetProperty(item => item.ReviewedAt, now)
                        .SetProperty(item => item.ReviewedByUserId, actorId)
                        .SetProperty(item => item.Reason, "Đơn thuê đã bị hủy."), cancellationToken);
                await _context.Payments
                    .Where(payment => payment.RentalOrderId == orderId && payment.Status == PaymentStatus.PENDING)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(payment => payment.Status, PaymentStatus.FAILED),
                        cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            order.Status = target;
            order.UpdatedAt = now;
            if (request.IsReview)
                order.RejectionReason = request.RejectionReason;
            foreach (var detail in order.RentalOrderDetails)
            {
                detail.Status = nextDetailStatus;
            }

            return MapToResponse(order);
        }

        private static void EnsureStartIsNotInPast(DateTime startUtc)
        {
            if (startUtc < DateTime.UtcNow)
            {
                throw new ArgumentException("The rental start cannot be in the past.");
            }
        }

        private static RentalOrderResponse MapToResponse(RentalOrder order)
        {
            return new RentalOrderResponse
            {
                Id = order.Id,
                OrderCode = order.OrderCode,
                CustomerId = order.CustomerId,
                StartDatetime = order.StartDatetime,
                EndDatetime = order.EndDatetime,
                TotalAmount = order.TotalAmount,
                Status = order.Status,
                CanUseManualPayment = order.CanUseManualPayment(),
                Note = order.Note,
                RejectionReason = order.RejectionReason,
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt,
                Details = order.RentalOrderDetails.Select(detail => new RentalOrderDetailResponse
                {
                    Id = detail.Id,
                    VehicleId = detail.VehicleId,
                    PricePerDay = detail.PricePerDay,
                    RentalDays = detail.RentalDays,
                    Subtotal = detail.Subtotal,
                    Status = detail.Status
                }).ToList()
            };
        }
    }
}
