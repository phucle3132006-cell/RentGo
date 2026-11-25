using CarRental.Models.DTOs.VehicleImages;

namespace CarRental.BLL.Interfaces
{
    public interface IVehicleImageService
    {
        Task<List<VehicleImageResponse>> GetByVehicleIdAsync(long vehicleId);

        Task<VehicleImageResponse> UploadAsync(
            long vehicleId,
            Stream fileStream,
            string fileName,
            long fileLength,
            bool isPrimary);

        Task<bool> SetPrimaryAsync(long vehicleId, long imageId);

        Task<bool> DeleteAsync(long vehicleId, long imageId);
    }
}
