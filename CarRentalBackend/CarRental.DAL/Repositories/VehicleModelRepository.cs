
using CarRental.DAL.Context;
using CarRental.DAL.Interfaces;
using CarRental.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace CarRental.DAL.Repositories
{
    // Truy xuất dữ liệu bảng vehicle_models.
    public class VehicleModelRepository : IVehicleModelRepository
    {
        private readonly CarRentalDbContext _context;

        // Nhận DbContext từ Dependency Injection.
        public VehicleModelRepository(CarRentalDbContext context)
        {
            _context = context;
        }

        // Lấy tất cả mẫu xe, sắp xếp theo ID.
        public async Task<List<VehicleModel>> GetAllAsync()
        {
            return await _context.VehicleModels
                .AsNoTracking()
                .OrderBy(vm => vm.Id)
                .ToListAsync();
        }

        // Tìm mẫu xe theo ID.
        // Có tracking để hỗ trợ cập nhật hoặc xóa.
        public async Task<VehicleModel?> GetByIdAsync(long id)
        {
            return await _context.VehicleModels
                .FirstOrDefaultAsync(vm => vm.Id == id);
        }

        // Thêm mẫu xe vào DbContext.
        public async Task AddAsync(VehicleModel vehicleModel)
        {
            await _context.VehicleModels.AddAsync(vehicleModel);
        }

        // Kiểm tra có xe thực tế thuộc mẫu xe này không.
        public async Task<bool> HasVehiclesAsync(long id)
        {
            return await _context.Vehicles
                .AnyAsync(v => v.VehicleModelId == id);
        }

        // Đánh dấu mẫu xe để xóa.
        public void Delete(VehicleModel vehicleModel)
        {
            _context.VehicleModels.Remove(vehicleModel);
        }

        // Lưu thay đổi xuống SQL Server.
        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}
