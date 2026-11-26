using System.Security.Claims;
using CarRental.BLL.Interfaces;
using CarRental.Models.DTOs.Payments;
using CarRental.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarRental.API.Controllers;

[ApiController]
[Route("api/rental-orders/{orderId:long}/payments")]
public sealed class PaymentsController(IPaymentService payments) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "CUSTOMER")]
    public async Task<IActionResult> Create(
        long orderId, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        [FromBody] CreatePaymentRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetActorId(out var actorId))
            return Unauthorized();

        if (!Guid.TryParseExact(idempotencyKey, "D", out var key) || key == Guid.Empty)
            return BadRequest(new { message = "Idempotency-Key must be a non-empty UUID." });

        try
        {
            var result = await payments.CreateAsync(orderId, actorId, key, request, cancellationToken);
            return result.Created
                ? CreatedAtAction(nameof(Get), new { orderId, paymentId = result.Payment.Id }, result.Payment)
                : Ok(result.Payment);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("confirm-manual")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> ConfirmManual(
        long orderId, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        [FromBody] ConfirmManualPaymentRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetActorId(out var actorId))
            return Unauthorized();

        if (!Guid.TryParseExact(idempotencyKey, "D", out var key) || key == Guid.Empty)
            return BadRequest(new { message = "Idempotency-Key must be a non-empty UUID." });

        try
        {
            var result = await payments.ConfirmManualAsync(orderId, actorId, key, request, cancellationToken);
            return result.Created
                ? CreatedAtAction(nameof(Get), new { orderId, paymentId = result.Payment.Id }, result.Payment)
                : Ok(result.Payment);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpGet("{paymentId:long}")]
    [Authorize(Roles = "CUSTOMER,ADMIN")]
    public async Task<IActionResult> Get(long orderId, long paymentId, CancellationToken cancellationToken)
    {
        if (!TryGetActorId(out var actorId))
            return Unauthorized();

        var role = User.IsInRole(nameof(UserRole.ADMIN)) ? UserRole.ADMIN : UserRole.CUSTOMER;
        try
        {
            return Ok(await payments.GetAsync(orderId, paymentId, actorId, role, cancellationToken));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    private bool TryGetActorId(out long actorId) =>
        long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out actorId) && actorId > 0;
}
