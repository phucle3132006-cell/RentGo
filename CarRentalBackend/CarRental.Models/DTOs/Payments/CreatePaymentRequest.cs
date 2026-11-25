using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CarRental.Models.DTOs.Payments;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CreatePaymentRequest
{
    [Required, StringLength(30)]
    public string PaymentMethod { get; init; } = string.Empty;
}
