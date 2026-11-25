using System.ComponentModel.DataAnnotations;
using CarRental.Models.Enums;

namespace CarRental.Models.DTOs.Vehicles;

public sealed class VehicleSearch : IValidatableObject
{
    public VehicleCategory? Category { get; set; }
    public VehicleStatus? Status { get; set; }
    public string? Brand { get; set; }
    public string? Model { get; set; }
    [Range(1, int.MaxValue)]
    public int? Seats { get; set; }
    public decimal? MaxPrice { get; set; }
    public DateTimeOffset? Start { get; set; }
    public DateTimeOffset? End { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MaxPrice is < 0 or > 99999999.99m)
            yield return new ValidationResult("Giá tối đa không hợp lệ.", [nameof(MaxPrice)]);

        if ((Start.HasValue || End.HasValue) &&
            (!Start.HasValue || !End.HasValue || End <= Start || Start < DateTimeOffset.UtcNow))
            yield return new ValidationResult(
                "Chọn đủ giờ nhận và trả xe. Giờ nhận phải ở tương lai và giờ trả phải sau giờ nhận.",
                [nameof(Start), nameof(End)]);
    }
}
