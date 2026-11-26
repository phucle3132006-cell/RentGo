using System;
using System.Collections.Generic;
using System.Text;
using CarRental.Models.Enums;

namespace CarRental.Models.Entities
{
    public class RentalOrder
    {
        public long Id { get; set; }

        public string OrderCode { get; set; } = string.Empty;

        public long CustomerId { get; set; }

        public DateTime StartDatetime { get; set; }

        public DateTime EndDatetime { get; set; }

        public decimal TotalAmount { get; set; }

        public RentalStatus Status { get; set; }

        public long? ReviewedByUserId { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? RejectionReason { get; set; }

        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        // Quan hệ với khách hàng
        public User Customer { get; set; } = null!;

        // Một đơn thuê có nhiều chi tiết xe
        public ICollection<RentalOrderDetail> RentalOrderDetails
        { get; set; } = new List<RentalOrderDetail>();

        // Một đơn thuê có thể có nhiều lần thanh toán
        public ICollection<Payment> Payments
        { get; set; } = new List<Payment>();

        public bool CanUseManualPayment() =>
            Status is RentalStatus.PENDING or RentalStatus.CONFIRMED &&
            TotalAmount > 0 && RentalOrderDetails.Count > 0 &&
            RentalOrderDetails.All(detail => detail.Status == RentalDetailStatus.RESERVED) &&
            Payments.All(payment => payment.Status != PaymentStatus.PAID);
    }
}

