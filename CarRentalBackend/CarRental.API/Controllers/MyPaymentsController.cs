using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using CarRental.DAL.Context;
using CarRental.Models.Entities;
using CarRental.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarRental.API.Controllers;

[ApiController]
[Authorize(Roles = "CUSTOMER")]
[Route("api/payments/my")]
public sealed class MyPaymentsController(CarRentalDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [Range(1, int.MaxValue / 20)] int page = 1, CancellationToken cancellationToken = default)
    {
        if (!TryGetCustomer(out var customerId))
            return Unauthorized();

        var items = await ForCustomer(customerId)
            .OrderByDescending(payment => payment.PaidAt)
            .ThenByDescending(payment => payment.Id)
            .Skip((page - 1) * 20)
            .Select(Projection)
            .Take(21).ToListAsync(cancellationToken);
        return Ok(new { items = items.Take(20), hasNext = items.Count > 20 });
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Detail(long id, CancellationToken cancellationToken)
    {
        if (!TryGetCustomer(out var customerId))
            return Unauthorized();

        var result = await ForCustomer(customerId).Where(payment => payment.Id == id)
            .Select(Projection).SingleOrDefaultAsync(cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    private IQueryable<Payment> ForCustomer(long customerId) => context.Payments.AsNoTracking()
        .Where(payment => payment.RentalOrder.CustomerId == customerId && payment.Status == PaymentStatus.PAID);

    private bool TryGetCustomer(out long id) =>
        long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out id) && id > 0;

    private static readonly System.Linq.Expressions.Expression<Func<Payment, HistoryItem>> Projection =
        payment => new HistoryItem(payment.Id, payment.RentalOrderId, payment.RentalOrder.OrderCode,
            payment.Amount, payment.PaymentMethod.ToString(), payment.TransactionCode, payment.ExternalReference,
            payment.PaidAt);

    public sealed record HistoryItem(long Id, long RentalOrderId, string OrderCode, decimal Amount,
        string PaymentMethod, string TransactionCode, string? ExternalReference, DateTime? PaidAt);
}
