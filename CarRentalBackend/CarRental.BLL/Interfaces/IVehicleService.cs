
using CarRental.Models.DTOs.Vehicles;

namespace CarRental.BLL.Interfaces
{
    public interface IVehicleService
    {
        Task<List<VehicleResponse>> GetAllAsync(VehicleSearch search, CancellationToken cancellationToken);

        Task<VehicleResponse?> GetByIdAsync(long id);

        Task<VehicleResponse> CreateAsync(
            CreateVehicleRequest request);

        Task<bool> UpdateAsync(
            long id,
            UpdateVehicleRequest request);

        Task<bool> DeleteAsync(long id);
    }
}
