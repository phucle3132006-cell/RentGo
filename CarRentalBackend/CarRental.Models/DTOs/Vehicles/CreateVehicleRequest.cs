
using System.ComponentModel.DataAnnotations;
using CarRental.Models.Enums;

namespace CarRental.Models.DTOs.Vehicles
{
    public class CreateVehicleRequest
    {
        [Range(1, long.MaxValue,
            ErrorMessage = "Vui long chon mau xe hop le.")]
        public long VehicleModelId { get; set; }

        [Required(ErrorMessage = "Bien so xe la bat buoc.")]
        [StringLength(20)]
        public string LicensePlate { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mau sac la bat buoc.")]
        [StringLength(30)]
        public string Color { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tinh trang xe la bat buoc.")]
        [StringLength(100)]
        public string Condition { get; set; } = string.Empty;

        [EnumDataType(typeof(VehicleStatus))]
        public VehicleStatus Status { get; set; }
            = VehicleStatus.AVAILABLE;
    }
}
