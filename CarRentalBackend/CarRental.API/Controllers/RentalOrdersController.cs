using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using CarRental.BLL.Interfaces;
using CarRental.Models.DTOs.RentalOrders;
using CarRental.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarRental.API.Controllers
{
    [ApiController]
    [Route("api/rental-orders")]
    public class RentalOrdersController : ControllerBase
    {
        private readonly IRentalOrderService _rentalOrderService;

        public RentalOrdersController(IRentalOrderService rentalOrderService)
        {
            _rentalOrderService = rentalOrderService;
        }

        [HttpPost]
        [Authorize(Roles = "CUSTOMER")]
        public async Task<IActionResult> Create(
            [FromBody] CreateRentalOrderRequest request,
            CancellationToken cancellationToken)
        {
            if (!TryGetActorId(out var customerId))
                return Unauthorized();

            try
            {
                var order = await _rentalOrderService.CreateAsync(customerId, request, cancellationToken);
                return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Khong tim thay xe duoc yeu cau." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (DbUpdateConcurrencyException)
            {
                return Conflict(new { message = "Du lieu da thay doi. Vui long thu lai." });
            }
        }

        [HttpGet("my")]
        [Authorize(Roles = "CUSTOMER")]
        public async Task<IActionResult> GetMyOrders(
            CancellationToken cancellationToken,
            [FromQuery, Range(1, int.MaxValue)] int page = 1,
            [FromQuery, Range(1, 100)] int pageSize = 20)
        {
            if (!TryGetActorId(out var customerId))
                return Unauthorized();

            try
            {
                var orders = await _rentalOrderService.ListByCustomerAsync(
                    customerId, page, pageSize, cancellationToken);
                return Ok(orders);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{id:long}")]
        [Authorize(Roles = "CUSTOMER,ADMIN")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            if (!TryGetActorId(out var actorId))
                return Unauthorized();

            if (id <= 0)
                return BadRequest(new { message = "ID don thue phai lon hon 0." });

            long? customerId = User.IsInRole(nameof(UserRole.ADMIN)) ? null : actorId;
            var order = await _rentalOrderService.GetByIdAsync(id, customerId, cancellationToken);
            return order is null ? OrderNotFound() : Ok(order);
        }

        [HttpPut("{id:long}/cancel")]
        [Authorize(Roles = "CUSTOMER,ADMIN")]
        public Task<IActionResult> Cancel(long id, CancellationToken cancellationToken)
        {
            return ChangeStatus(
                id, new UpdateRentalOrderStatusRequest { Status = RentalStatus.CANCELLED },
                cancellationToken);
        }

        [HttpPut("{id:long}/status")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> UpdateStatus(
            long id,
            [FromBody] UpdateRentalOrderStatusRequest request,
            CancellationToken cancellationToken)
        {
            if (request.Status is not (RentalStatus.RENTING or RentalStatus.COMPLETED))
            {
                return BadRequest(new { message = "Chi chap nhan trang thai RENTING hoac COMPLETED." });
            }

            return await ChangeStatus(id, request, cancellationToken);
        }

        private async Task<IActionResult> ChangeStatus(
            long id, UpdateRentalOrderStatusRequest request, CancellationToken cancellationToken)
        {
            if (!TryGetActorId(out var actorId))
                return Unauthorized();

            try
            {
                var order = await _rentalOrderService.UpdateStatusAsync(
                    id, actorId, request, cancellationToken);
                return Ok(order);
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
                return OrderNotFound();
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (DbUpdateConcurrencyException)
            {
                return Conflict(new { message = "Du lieu da thay doi. Vui long thu lai." });
            }
        }

        private bool TryGetActorId(out long actorId)
        {
            return long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out actorId)
                && actorId > 0;
        }

        private NotFoundObjectResult OrderNotFound()
        {
            return NotFound(new { message = "Khong tim thay don thue." });
        }
    }
}
