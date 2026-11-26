namespace CarRental.Models.DTOs.RentalOrders
{
    public class RentalOrderListResponse
    {
        public List<RentalOrderResponse> Items { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
    }
}
