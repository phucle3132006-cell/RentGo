using CarRental.Models.DTOs.VehicleModels;

namespace CarRental.BLL.Interfaces
{
    public interface IVehicleModelService
    {
        // Lấy danh sách tất cả mẫu xe.
        Task<List<VehicleModelResponse>> GetAllAsync();

        // Lấy thông tin một mẫu xe theo ID.
        Task<VehicleModelResponse?> GetByIdAsync(long id);

        // Thêm mẫu xe mới.
        Task<VehicleModelResponse> CreateAsync(
            CreateVehicleModelRequest request);

        // Cập nhật thông tin mẫu xe.
        Task<bool> UpdateAsync(
            long id,
            UpdateVehicleModelRequest request);

        // Xóa mẫu xe nếu không có xe thực tế liên kết.
        Task<bool> DeleteAsync(long id);
    }
}