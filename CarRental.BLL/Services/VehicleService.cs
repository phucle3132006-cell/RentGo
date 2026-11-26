
using CarRental.BLL.Interfaces;
using CarRental.DAL.Interfaces;
using CarRental.Models.DTOs.Vehicles;
using CarRental.Models.Entities;
using CarRental.Models.Enums;

namespace CarRental.BLL.Services
{
    public class VehicleService : IVehicleService
    {
        private readonly IVehicleRepository _vehicleRepository;

        public VehicleService(
            IVehicleRepository vehicleRepository)
        {
            _vehicleRepository = vehicleRepository;
        }

        public async Task<List<VehicleResponse>> GetAllAsync(VehicleSearch search, CancellationToken cancellationToken)
        {
            var vehicles = await _vehicleRepository.GetAllAsync(search, cancellationToken);

            return vehicles.Select(MapToResponse).ToList();
        }

        public async Task<VehicleResponse?> GetByIdAsync(long id)
        {
            var vehicle = await _vehicleRepository.GetByIdAsync(id);

            if (vehicle == null)
                return null;

            return MapToResponse(vehicle);
        }

        public async Task<VehicleResponse> CreateAsync(
            CreateVehicleRequest request)
        {
            ValidateVehicle(
                request.VehicleModelId,
                request.LicensePlate,
                request.Color,
                request.Condition,
                request.Status);

            bool modelExists =
                await _vehicleRepository.VehicleModelExistsAsync(
                    request.VehicleModelId);

            if (!modelExists)
            {
                throw new ArgumentException(
                    "Mau xe khong ton tai.");
            }

            string licensePlate =
                request.LicensePlate.Trim().ToUpperInvariant();

            bool plateExists =
                await _vehicleRepository.LicensePlateExistsAsync(
                    licensePlate);

            if (plateExists)
            {
                throw new InvalidOperationException(
                    "Bien so xe da ton tai.");
            }

            var now = DateTime.UtcNow;

            var vehicle = new Vehicle
            {
                VehicleModelId = request.VehicleModelId,
                LicensePlate = licensePlate,
                Color = request.Color.Trim(),
                Condition = request.Condition.Trim(),
                Status = request.Status,
                CreatedAt = now,
                UpdatedAt = now
            };

            await _vehicleRepository.AddAsync(vehicle);
            await _vehicleRepository.SaveChangesAsync();

            // Lay lai Entity kem thong tin mau xe.
            var createdVehicle =
                await _vehicleRepository.GetByIdAsync(vehicle.Id);

            if (createdVehicle == null)
            {
                throw new InvalidOperationException(
                    "Khong the tai lai xe vua tao.");
            }

            return MapToResponse(createdVehicle);
        }

        public async Task<bool> UpdateAsync(
            long id,
            UpdateVehicleRequest request)
        {
            var vehicle = await _vehicleRepository.GetByIdAsync(id);

            if (vehicle == null)
                return false;

            ValidateVehicle(
                request.VehicleModelId,
                request.LicensePlate,
                request.Color,
                request.Condition,
                request.Status);

            bool modelExists =
                await _vehicleRepository.VehicleModelExistsAsync(
                    request.VehicleModelId);

            if (!modelExists)
            {
                throw new ArgumentException(
                    "Mau xe khong ton tai.");
            }

            string licensePlate =
                request.LicensePlate.Trim().ToUpperInvariant();

            bool plateExists =
                await _vehicleRepository.LicensePlateExistsAsync(
                    licensePlate,
                    id);

            if (plateExists)
            {
                throw new InvalidOperationException(
                    "Bien so xe da ton tai.");
            }

            vehicle.VehicleModelId = request.VehicleModelId;
            vehicle.LicensePlate = licensePlate;
            vehicle.Color = request.Color.Trim();
            vehicle.Condition = request.Condition.Trim();
            vehicle.Status = request.Status;
            vehicle.UpdatedAt = DateTime.UtcNow;

            _vehicleRepository.Update(vehicle);
            await _vehicleRepository.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteAsync(long id)
        {
            var vehicle = await _vehicleRepository.GetByIdAsync(id);

            if (vehicle == null)
                return false;

            bool hasRentalHistory =
                await _vehicleRepository.HasRentalOrderDetailsAsync(id);

            if (hasRentalHistory)
            {
                throw new InvalidOperationException(
                    "Khong the xoa xe da co lich su thue.");
            }

            _vehicleRepository.Delete(vehicle);
            await _vehicleRepository.SaveChangesAsync();

            return true;
        }

        private static void ValidateVehicle(
            long vehicleModelId,
            string licensePlate,
            string color,
            string condition,
            VehicleStatus status)
        {
            if (vehicleModelId <= 0)
            {
                throw new ArgumentException(
                    "VehicleModelId khong hop le.");
            }

            if (string.IsNullOrWhiteSpace(licensePlate))
            {
                throw new ArgumentException(
                    "Bien so xe khong duoc de trong.");
            }

            if (licensePlate.Trim().Length > 20)
            {
                throw new ArgumentException(
                    "Bien so xe toi da 20 ky tu.");
            }

            if (string.IsNullOrWhiteSpace(color) ||
                color.Trim().Length > 30)
            {
                throw new ArgumentException(
                    "Mau sac khong hop le.");
            }

            if (string.IsNullOrWhiteSpace(condition) ||
                condition.Trim().Length > 100)
            {
                throw new ArgumentException(
                    "Tinh trang xe khong hop le.");
            }

            if (!Enum.IsDefined(typeof(VehicleStatus), status))
            {
                throw new ArgumentException(
                    "Trang thai xe khong hop le.");
            }
        }

        private static VehicleResponse MapToResponse(Vehicle vehicle)
        {
            return new VehicleResponse
            {
                Id = vehicle.Id,
                VehicleModelId = vehicle.VehicleModelId,
                Brand = vehicle.VehicleModel.Brand,
                Model = vehicle.VehicleModel.Model,
                LicensePlate = vehicle.LicensePlate,
                Color = vehicle.Color,
                Condition = vehicle.Condition,
                Status = vehicle.Status,
                CreatedAt = vehicle.CreatedAt,
                UpdatedAt = vehicle.UpdatedAt
            };
        }
    }
}
