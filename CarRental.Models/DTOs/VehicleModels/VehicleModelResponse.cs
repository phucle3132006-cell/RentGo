using CarRental.Models.Enums;
namespace CarRental.Models.DTOs.VehicleModels
{
    public class VehicleModelResponse
    {
        public long Id { get; set; }
        public VehicleCategory Category { get; set; }
        public string Brand { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int Year { get; set; }
        public int Seats { get; set; }
        public int? EngineCapacity { get; set; }
        public string? Description { get; set; }
        public decimal PricePerDay { get; set; }
    }
}