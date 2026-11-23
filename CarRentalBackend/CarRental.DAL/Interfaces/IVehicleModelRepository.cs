using CarRental.Models.Entities;

namespace CarRental.DAL.Interfaces
{
    public interface IVehicleModelRepository
    {
        Task<List<VehicleModel>> GetAllAsync();
        Task<VehicleModel?> GetByIdAsync(long id);
        Task AddAsync(VehicleModel vehicleModel);
        Task<bool> HasVehiclesAsync(long id);
        void Delete(VehicleModel vehicleModel);
        Task<int> SaveChangesAsync();
    }
}