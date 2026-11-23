using System.ComponentModel.DataAnnotations;
using CarRental.Models.Enums;

namespace CarRental.Models.DTOs.RentalOrders
{
    public class UpdateRentalOrderStatusRequest
    {
        [System.Text.Json.Serialization.JsonIgnore]
        public bool IsReview { get; init; }

        [System.Text.Json.Serialization.JsonIgnore]
        public string? RejectionReason { get; init; }

        [Required]
        [EnumDataType(typeof(RentalStatus))]
        public RentalStatus? Status { get; set; }
    }
}
