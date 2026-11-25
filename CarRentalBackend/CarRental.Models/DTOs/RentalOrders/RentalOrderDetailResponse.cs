using CarRental.Models.Enums;

namespace CarRental.Models.DTOs.RentalOrders
{
    public class RentalOrderDetailResponse
    {
        public long Id { get; set; }
        public long VehicleId { get; set; }
        public decimal PricePerDay { get; set; }
        public int RentalDays { get; set; }
        public decimal Subtotal { get; set; }
        public RentalDetailStatus Status { get; set; }
    }
}
