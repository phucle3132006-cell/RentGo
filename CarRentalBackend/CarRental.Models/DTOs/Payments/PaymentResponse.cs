namespace CarRental.Models.DTOs.Payments;

public sealed record PaymentResponse(
    long Id,
    long RentalOrderId,
    decimal Amount,
    string PaymentMethod,
    string TransactionCode,
    string Status,
    DateTime? PaidAt,
    DateTime CreatedAt);
