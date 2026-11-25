using CarRental.BLL.Interfaces;
using CarRental.DAL.Interfaces;
using CarRental.Models.DTOs.VehicleModels;
using CarRental.Models.Entities;
using System.Linq;

namespace CarRental.BLL.Services
{
    public class VehicleModelService : IVehicleModelService
    {
        private readonly IVehicleModelRepository _repository;

        public VehicleModelService(IVehicleModelRepository repository)
        {
            _repository = repository;
        }

        // Lấy danh sách tất cả mẫu xe.
        public async Task<List<VehicleModelResponse>> GetAllAsync()
        {
            var models = await _repository.GetAllAsync();

            return models.Select(ToResponse).ToList();
        }

        // Lấy mẫu xe theo ID.
        public async Task<VehicleModelResponse?> GetByIdAsync(long id)
        {
            var model = await _repository.GetByIdAsync(id);

            return model == null ? null : ToResponse(model);
        }

        // Thêm mẫu xe mới.
        public async Task<VehicleModelResponse> CreateAsync(
            CreateVehicleModelRequest request)
        {
            ValidateRequest(
                request.Brand,
                request.Model,
                request.Year,
                request.Seats,
                request.PricePerDay);

            var models = await _repository.GetAllAsync();

            bool exists = models.Any(x =>
                x.Brand.Equals(request.Brand.Trim(),
                    StringComparison.OrdinalIgnoreCase) &&
                x.Model.Equals(request.Model.Trim(),
                    StringComparison.OrdinalIgnoreCase) &&
                x.Year == request.Year);

            if (exists)
            {
                throw new InvalidOperationException(
                    "Mẫu xe với hãng, tên và năm sản xuất này đã tồn tại.");
            }

            var now = DateTime.UtcNow;

            var entity = new VehicleModel
            {
                Category = request.Category,
                Brand = request.Brand.Trim(),
                Model = request.Model.Trim(),
                Year = request.Year,
                Seats = request.Seats,
                EngineCapacity = request.EngineCapacity,
                Description = request.Description?.Trim(),
                PricePerDay = request.PricePerDay,
                CreatedAt = now,
                UpdatedAt = now
            };

            await _repository.AddAsync(entity);
            await _repository.SaveChangesAsync();

            return ToResponse(entity);
        }

        // Cập nhật thông tin mẫu xe.
        public async Task<bool> UpdateAsync(
            long id,
            UpdateVehicleModelRequest request)
        {
            var entity = await _repository.GetByIdAsync(id);

            if (entity == null)
                return false;

            ValidateRequest(
                request.Brand,
                request.Model,
                request.Year,
                request.Seats,
                request.PricePerDay);

            var models = await _repository.GetAllAsync();

            bool exists = models.Any(x =>
                x.Id != id &&
                x.Brand.Equals(request.Brand.Trim(),
                    StringComparison.OrdinalIgnoreCase) &&
                x.Model.Equals(request.Model.Trim(),
                    StringComparison.OrdinalIgnoreCase) &&
                x.Year == request.Year);

            if (exists)
            {
                throw new InvalidOperationException(
                    "Mẫu xe với hãng, tên và năm sản xuất này đã tồn tại.");
            }

            entity.Category = request.Category;
            entity.Brand = request.Brand.Trim();
            entity.Model = request.Model.Trim();
            entity.Year = request.Year;
            entity.Seats = request.Seats;
            entity.EngineCapacity = request.EngineCapacity;
            entity.Description = request.Description?.Trim();
            entity.PricePerDay = request.PricePerDay;
            entity.UpdatedAt = DateTime.UtcNow;

            await _repository.SaveChangesAsync();

            return true;
        }

        // Xóa mẫu xe khi không có xe thực tế liên kết.
        public async Task<bool> DeleteAsync(long id)
        {
            var entity = await _repository.GetByIdAsync(id);

            if (entity == null)
                return false;

            if (await _repository.HasVehiclesAsync(id))
            {
                throw new InvalidOperationException(
                    "Không thể xóa mẫu xe vì đang có xe thực tế liên kết.");
            }

            _repository.Delete(entity);
            await _repository.SaveChangesAsync();

            return true;
        }

        // Kiểm tra dữ liệu nghiệp vụ.
        private static void ValidateRequest(
            string brand,
            string model,
            int year,
            int seats,
            decimal pricePerDay)
        {
            if (string.IsNullOrWhiteSpace(brand))
                throw new ArgumentException("Hãng xe không được để trống.");

            if (string.IsNullOrWhiteSpace(model))
                throw new ArgumentException("Tên mẫu xe không được để trống.");

            if (year < 1900 || year > 2100)
                throw new ArgumentException("Năm sản xuất không hợp lệ.");

            if (seats < 1 || seats > 100)
                throw new ArgumentException("Số chỗ ngồi không hợp lệ.");

            if (pricePerDay <= 0)
                throw new ArgumentException("Giá thuê mỗi ngày phải lớn hơn 0.");
        }

        // Chuyển Entity sang DTO.
        private static VehicleModelResponse ToResponse(VehicleModel entity)
        {
            return new VehicleModelResponse
            {
                Id = entity.Id,
                Category = entity.Category,
                Brand = entity.Brand,
                Model = entity.Model,
                Year = entity.Year,
                Seats = entity.Seats,
                EngineCapacity = entity.EngineCapacity,
                Description = entity.Description,
                PricePerDay = entity.PricePerDay
            };
        }
    }
}