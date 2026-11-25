using CarRental.Models.DTOs.Vehicles;

using CarRental.Models.Entities;

namespace CarRental.DAL.Interfaces
{
    public interface IVehicleRepository
    {
        Task<List<Vehicle>> GetAllAsync(VehicleSearch search, CancellationToken cancellationToken);

        Task<Vehicle?> GetByIdAsync(long id);

        Task<bool> LicensePlateExistsAsync(
            string licensePlate,
            long? excludeId = null);

        Task<bool> VehicleModelExistsAsync(long vehicleModelId);
        Task<bool> HasRentalOrderDetailsAsync(long vehicleId);

        Task AddAsync(Vehicle vehicle);

        void Update(Vehicle vehicle);

        void Delete(Vehicle vehicle);

        Task<int> SaveChangesAsync();
    }
}
