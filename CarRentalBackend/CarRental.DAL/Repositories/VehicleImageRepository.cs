using CarRental.DAL.Context;
using CarRental.DAL.Interfaces;
using CarRental.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace CarRental.DAL.Repositories
{
    public class VehicleImageRepository : IVehicleImageRepository
    {
        private readonly CarRentalDbContext _context;

        public VehicleImageRepository(CarRentalDbContext context)
        {
            _context = context;
        }

        public Task<List<VehicleImage>> GetByVehicleIdAsync(long vehicleId)
        {
            return _context.VehicleImages
                .AsNoTracking()
                .Where(image => image.VehicleId == vehicleId)
                .OrderByDescending(image => image.IsPrimary)
                .ThenBy(image => image.Id)
                .ToListAsync();
        }

        public Task<VehicleImage?> GetByIdAsync(long id)
        {
            return _context.VehicleImages
                .FirstOrDefaultAsync(image => image.Id == id);
        }

        public Task<bool> VehicleExistsAsync(long vehicleId)
        {
            return _context.Vehicles.AnyAsync(vehicle => vehicle.Id == vehicleId);
        }

        public Task<bool> ImageUrlExistsAsync(long vehicleId, string imageUrl)
        {
            return _context.VehicleImages.AnyAsync(image =>
                image.VehicleId == vehicleId && image.ImageUrl == imageUrl);
        }

        public Task<VehicleImage?> GetPrimaryImageAsync(long vehicleId)
        {
            return _context.VehicleImages.FirstOrDefaultAsync(image =>
                image.VehicleId == vehicleId && image.IsPrimary);
        }

        public async Task AddAsync(VehicleImage vehicleImage)
        {
            await _context.VehicleImages.AddAsync(vehicleImage);
        }

        public void Update(VehicleImage vehicleImage)
        {
            _context.VehicleImages.Update(vehicleImage);
        }

        public void Delete(VehicleImage vehicleImage)
        {
            _context.VehicleImages.Remove(vehicleImage);
        }

        public Task<int> SaveChangesAsync()
        {
            return _context.SaveChangesAsync();
        }

        public async Task<T> ExecuteForVehicleAsync<T>(long vehicleId, Func<bool, Task<T>> operation)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Serialize image mutations for the same vehicle across application instances.
                var vehicleIds = await _context.Database.SqlQuery<long>(
                    $"SELECT [id] AS [Value] FROM [dbo].[vehicles] WITH (UPDLOCK, HOLDLOCK) WHERE [id] = {vehicleId}")
                    .ToListAsync();

                var result = await operation(vehicleIds.Count != 0);
                await transaction.CommitAsync();
                return result;
            }
            catch
            {
                // Disposing the transaction rolls back unfinished work. Drop tracked image
                // state because multiple saves may have occurred before rollback.
                foreach (var entry in _context.ChangeTracker.Entries<VehicleImage>().ToList())
                    entry.State = EntityState.Detached;

                throw;
            }
        }
    }
}
