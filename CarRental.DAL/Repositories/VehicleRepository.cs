using CarRental.Models.Enums;
using CarRental.Models.DTOs.Vehicles;

using CarRental.DAL.Context;
using CarRental.DAL.Interfaces;
using CarRental.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace CarRental.DAL.Repositories
{
    public class VehicleRepository : IVehicleRepository
    {
        private readonly CarRentalDbContext _context;

        public VehicleRepository(CarRentalDbContext context)
        {
            _context = context;
        }

        public async Task<List<Vehicle>> GetAllAsync(VehicleSearch search, CancellationToken cancellationToken)
        {
            var query = _context.Vehicles.AsNoTracking().Include(v => v.VehicleModel).AsQueryable();
            if (search.Category is { } category)
                query = query.Where(v => v.VehicleModel.Category == category);
            if (search.Status is { } status)
                query = query.Where(v => v.Status == status);
            if (!string.IsNullOrWhiteSpace(search.Brand))
                query = query.Where(v => v.VehicleModel.Brand == search.Brand);
            if (!string.IsNullOrWhiteSpace(search.Model))
                query = query.Where(v => v.VehicleModel.Model == search.Model);
            if (search.Seats is { } seats)
                query = query.Where(v => v.VehicleModel.Seats == seats);
            if (search.MaxPrice is { } maxPrice)
                query = query.Where(v => v.VehicleModel.PricePerDay <= maxPrice);
            if (search.Start is { } start && search.End is { } end)
            {
                var conflicts = RentalOrderRepository.OverlappingDetails(
                    _context.RentalOrderDetails, start.UtcDateTime, end.UtcDateTime);
                query = query.Where(v => v.Status != VehicleStatus.MAINTENANCE &&
                    !conflicts.Any(detail => detail.VehicleId == v.Id));
            }

            return await query.OrderBy(v => v.Id).ToListAsync(cancellationToken);
        }

        public async Task<Vehicle?> GetByIdAsync(long id)
        {
            return await _context.Vehicles
                .Include(v => v.VehicleModel)
                .FirstOrDefaultAsync(v => v.Id == id);
        }

        public async Task<bool> LicensePlateExistsAsync(
            string licensePlate,
            long? excludeId = null)
        {
            return await _context.Vehicles.AnyAsync(v =>
                v.LicensePlate == licensePlate &&
                (!excludeId.HasValue || v.Id != excludeId.Value));
        }

        public async Task<bool> VehicleModelExistsAsync(long vehicleModelId)
        {
            return await _context.VehicleModels
                .AnyAsync(vm => vm.Id == vehicleModelId);
        }
        public async Task<bool> HasRentalOrderDetailsAsync(long vehicleId)
        {
            return await _context.RentalOrderDetails
                .AnyAsync(d => d.VehicleId == vehicleId);
        }
        public async Task AddAsync(Vehicle vehicle)
        {
            await _context.Vehicles.AddAsync(vehicle);
        }

        public void Update(Vehicle vehicle)
        {
            _context.Vehicles.Update(vehicle);
        }

        public void Delete(Vehicle vehicle)
        {
            _context.Vehicles.Remove(vehicle);
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}
