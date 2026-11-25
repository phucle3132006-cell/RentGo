using System;
using System.Collections.Generic;
using System.Text;
using CarRental.Models.Enums;

namespace CarRental.Models.Entities
{
    public class Payment
    {
        public long Id { get; set; }

        public long RentalOrderId { get; set; }

        public decimal Amount { get; set; }

        public PaymentMethod PaymentMethod { get; set; }

        public string TransactionCode { get; set; } = string.Empty;

        public PaymentStatus Status { get; set; }

        public long? ConfirmedByUserId { get; set; }
        public string? ExternalReference { get; set; }
        public DateTime? PaidAt { get; set; }

        public DateTime CreatedAt { get; set; }

        // Quan hệ với đơn thuê
        public RentalOrder RentalOrder { get; set; } = null!;
    }
}

