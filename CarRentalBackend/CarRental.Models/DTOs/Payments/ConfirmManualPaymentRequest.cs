namespace CarRental.Models.DTOs.Payments;

public sealed record ConfirmManualPaymentRequest(
    decimal Amount, string PaymentMethod, string? ExternalReference, bool FundsVerified, long? TransferRequestId = null);
