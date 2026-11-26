namespace CarRental.Models.Entities;

public sealed class UserBlacklistEntry
{
    public long UserId { get; set; }
    public User User { get; set; } = null!;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? PhoneKey { get; private set; }
    public string Reason { get; set; } = string.Empty;
    public long? BlockedByUserId { get; set; }
    public User? BlockedBy { get; set; }
    public DateTime? BlockedAt { get; set; }
}
