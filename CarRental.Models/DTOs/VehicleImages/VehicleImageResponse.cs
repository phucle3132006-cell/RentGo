namespace CarRental.Models.DTOs.VehicleImages;

public class VehicleImageResponse
{
    public long Id { get; set; }
    public long VehicleId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;    
    public bool IsPrimary { get; set; }
    public DateTime CreatedAt { get; set; }
}
