namespace CarRental.Models.Entities;

public sealed class TransferRequest
{
    public long Id { get; set; }
    public long OrderId { get; set; }
    public long CustomerId { get; set; }
    public string PaymentMethod { get; set; } = "BANK_TRANSFER";
    public Guid SubmissionKey { get; set; }
    public string Status { get; set; } = "PENDING";
    public DateTime SubmittedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public long? ReviewedByUserId { get; set; }
    public long? PaymentId { get; set; }
    public string? Reason { get; set; }
    public RentalOrder Order { get; set; } = null!;
}
