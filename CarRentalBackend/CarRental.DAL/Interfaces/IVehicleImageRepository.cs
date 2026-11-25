using CarRental.Models.Entities;

namespace CarRental.DAL.Interfaces
{
    public interface IVehicleImageRepository
    {
        Task<List<VehicleImage>> GetByVehicleIdAsync(long vehicleId);

        Task<VehicleImage?> GetByIdAsync(long id);

        Task<bool> VehicleExistsAsync(long vehicleId);

        Task<bool> ImageUrlExistsAsync(long vehicleId, string imageUrl);

        Task<VehicleImage?> GetPrimaryImageAsync(long vehicleId);

        Task AddAsync(VehicleImage vehicleImage);

        void Update(VehicleImage vehicleImage);

        void Delete(VehicleImage vehicleImage);

        Task<int> SaveChangesAsync();

        Task<T> ExecuteForVehicleAsync<T>(long vehicleId, Func<bool, Task<T>> operation);
    }
}
