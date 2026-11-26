using System.ComponentModel.DataAnnotations;
using CarRental.Models.Enums;
namespace CarRental.Models.DTOs.VehicleModels
{
    public class CreateVehicleModelRequest
    {
        [Required]
        public VehicleCategory Category { get; set; }

        [Required]
        public string Brand { get; set; } = string.Empty;

        [Required]
        public string Model { get; set; } = string.Empty;

        [Range(1900, 2100)]
        public int Year { get; set; }

        [Range(1, 100)]
        public int Seats { get; set; }

        [Range(1, 10000)]
        public int? EngineCapacity { get; set; }

        public string? Description { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal PricePerDay { get; set; }
    }
}