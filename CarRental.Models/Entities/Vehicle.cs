using CarRental.Models.Enums;

namespace CarRental.Models.Entities
{
    public class Vehicle
    {
        public long Id { get; set; }

        // Khóa ngoại đến bảng vehicle_models.
        public long VehicleModelId { get; set; }

        public string LicensePlate { get; set; } = string.Empty;

        public string Color { get; set; } = string.Empty;

        public string Condition { get; set; } = string.Empty;

        public VehicleStatus Status { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        // Mỗi xe thuộc về một dòng xe.
        public VehicleModel VehicleModel { get; set; } = null!;

        // Một xe có thể có nhiều hình ảnh.
        public ICollection<VehicleImage> VehicleImages { get; set; }
            = new List<VehicleImage>();

        // Một xe có thể xuất hiện trong nhiều
        // chi tiết đơn thuê khác nhau theo thời gian.
        public ICollection<RentalOrderDetail> RentalOrderDetails
        { get; set; } = new List<RentalOrderDetail>();
    }
}