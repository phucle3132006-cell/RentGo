 using System;
using System.Collections.Generic;
using System.Text;
using CarRental.Models.Enums;

namespace CarRental.Models.Entities
{
    public class RentalOrderDetail
    {
        public long Id { get; set; }

        public long RentalOrderId { get; set; }

        public long VehicleId { get; set; }

        public decimal PricePerDay { get; set; }

        public int RentalDays { get; set; }

        public decimal Subtotal { get; set; }

        public RentalDetailStatus Status { get; set; }

        public DateTime CreatedAt { get; set; }

        // Quan hệ với đơn thuê
        public RentalOrder RentalOrder { get; set; } = null!;

        // Quan hệ với xe
        public Vehicle Vehicle { get; set; } = null!;
    }
}
