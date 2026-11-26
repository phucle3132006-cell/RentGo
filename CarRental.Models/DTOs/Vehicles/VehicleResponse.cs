
using CarRental.Models.Enums;

namespace CarRental.Models.DTOs.Vehicles
{
    public class VehicleResponse
    {
        public long Id { get; set; }

        public long VehicleModelId { get; set; }

        public string Brand { get; set; } = string.Empty;

        public string Model { get; set; } = string.Empty;

        public string LicensePlate { get; set; } = string.Empty;

        public string Color { get; set; } = string.Empty;

        public string Condition { get; set; } = string.Empty;

        public VehicleStatus Status { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
