using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using CarRental.BLL.Interfaces;
using CarRental.DAL.Context;
using CarRental.Models.Entities;
using CarRental.Models.DTOs.RentalOrders;
using CarRental.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarRental.API.Controllers;

[ApiController]
[Authorize(Roles = "ADMIN")]
[Route("api/rental-orders")]
public sealed class AdminRentalOrdersController(CarRentalDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        CancellationToken cancellationToken,
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 20,
        [FromQuery] RentalStatus? status = null,
        [FromQuery] bool awaitingProcessing = false,
        [FromQuery, StringLength(100)] string? search = null)
    {
        if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var adminId) || adminId <= 0)
            return Unauthorized();

        var offset = (page - 1L) * pageSize;
        if (offset > int.MaxValue || (status.HasValue && !Enum.IsDefined(status.Value)))
            return BadRequest(new { message = "Trang hoặc trạng thái đơn không hợp lệ." });

        if (!await context.Users.AnyAsync(user => user.Id == adminId &&
            user.Role == UserRole.ADMIN && user.Status == UserStatus.ACTIVE, cancellationToken))
            return Forbid();

        var orders = context.RentalOrders.AsNoTracking();
        if (awaitingProcessing)
            orders = orders.Where(order => order.Status == RentalStatus.PENDING || order.Status == RentalStatus.CONFIRMED);
        if (status.HasValue)
            orders = orders.Where(order => order.Status == status.Value);

        var term = search?.Trim();
        if (!string.IsNullOrEmpty(term))
        {
            long.TryParse(term, out var orderId);
            orders = orders.Where(order => order.Id == orderId ||
                order.OrderCode.Contains(term) || order.Customer.FullName.Contains(term) ||
                order.Customer.Email.Contains(term) || order.Customer.Phone.Contains(term));
        }

        var totalCount = await orders.CountAsync(cancellationToken);
        var items = await orders
            .OrderByDescending(order => order.CreatedAt)
            .ThenByDescending(order => order.Id)
            .Skip((int)offset)
            .Take(pageSize)
            .Select(order => new
            {
                order.Id,
                order.OrderCode,
                order.CustomerId,
                CustomerName = order.Customer.FullName,
                CustomerEmail = order.Customer.Email,
                CustomerPhone = order.Customer.Phone,
                PendingPaymentRequestId = context.Set<TransferRequest>()
                    .Where(item => item.OrderId == order.Id && item.Status == "PENDING")
                    .Select(item => (long?)item.Id).FirstOrDefault(),
                PendingPaymentMethod = context.Set<TransferRequest>()
                    .Where(item => item.OrderId == order.Id && item.Status == "PENDING")
                    .Select(item => item.PaymentMethod).FirstOrDefault(),
                order.StartDatetime,
                order.EndDatetime,
                order.TotalAmount,
                order.Status,
                order.CreatedAt,
                order.ReviewedAt,
                order.ReviewedByUserId,
                order.RejectionReason
            })
            .ToListAsync(cancellationToken);

        return Ok(new { items, page, pageSize, totalCount });
    }
}
