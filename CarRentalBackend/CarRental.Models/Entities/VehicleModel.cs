using CarRental.Models.Enums;

namespace CarRental.Models.Entities
{
    public class VehicleModel
    {
        public long Id { get; set; }

        public VehicleCategory Category { get; set; }

        public string Brand { get; set; } = string.Empty;

        public string Model { get; set; } = string.Empty;

        public int Year { get; set; }

        public int Seats { get; set; }

        // Có thể NULL theo cấu trúc SQL.
        public int? EngineCapacity { get; set; }

        // Có thể NULL theo cấu trúc SQL.
        public string? Description { get; set; }

        public decimal PricePerDay { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        // Một dòng xe có thể có nhiều xe thực tế.
        public ICollection<Vehicle> Vehicles { get; set; }
            = new List<Vehicle>();
    }
}