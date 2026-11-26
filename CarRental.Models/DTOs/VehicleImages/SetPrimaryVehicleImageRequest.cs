using System.ComponentModel.DataAnnotations;

namespace CarRental.Models.DTOs.VehicleImages;

public class SetPrimaryVehicleImageRequest
{
    [Range(1, long.MaxValue)]
    public long ImageId { get; set; }
}
