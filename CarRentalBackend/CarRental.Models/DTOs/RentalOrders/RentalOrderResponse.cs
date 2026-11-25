using CarRental.Models.Enums;

namespace CarRental.Models.DTOs.RentalOrders
{
    public class RentalOrderResponse
    {
        public long Id { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public long CustomerId { get; set; }
        public DateTime StartDatetime { get; set; }
        public DateTime EndDatetime { get; set; }
        public decimal TotalAmount { get; set; }
        public RentalStatus Status { get; set; }
        public bool CanUseManualPayment { get; set; }
        public string? RejectionReason { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<RentalOrderDetailResponse> Details { get; set; } = new();
    }
}
