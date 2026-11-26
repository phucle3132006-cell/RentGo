using System.ComponentModel.DataAnnotations;

namespace CarRental.Models.DTOs.RentalOrders
{
    public class CreateRentalOrderRequest : IValidatableObject
    {
        [Required]
        [MinLength(1, ErrorMessage = "Vui long chon it nhat mot xe.")]
        public List<long> VehicleIds { get; set; } = new();

        [Required]
        public DateTimeOffset? StartDatetime { get; set; }

        [Required]
        public DateTimeOffset? EndDatetime { get; set; }

        public string? Note { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (VehicleIds is not null && VehicleIds.Any(id => id <= 0))
            {
                yield return new ValidationResult(
                    "ID xe phai lon hon 0.", new[] { nameof(VehicleIds) });
            }

            if (VehicleIds is not null && VehicleIds.Distinct().Count() != VehicleIds.Count)
            {
                yield return new ValidationResult(
                    "Danh sach xe khong duoc trung lap.", new[] { nameof(VehicleIds) });
            }

            if (StartDatetime.HasValue && EndDatetime.HasValue &&
                EndDatetime.Value <= StartDatetime.Value)
            {
                yield return new ValidationResult(
                    "Thoi gian ket thuc phai sau thoi gian bat dau.",
                    new[] { nameof(EndDatetime) });
            }
        }
    }
}
