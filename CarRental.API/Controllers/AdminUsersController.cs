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
[Authorize(Roles = "ADMIN")]
[Route("api/admin/users")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AdminUsersController(CarRentalDbContext context) : ControllerBase
{
    public sealed record StatusRequest([Required] bool? Blocked, [StringLength(500)] string? Reason);

    private IQueryable<UserView> Project(IQueryable<User> users) =>
        users.Select(user => new UserView(user.Id, user.FullName, user.Email, user.Phone,
            user.Role.ToString(), user.Status.ToString(), user.CreatedAt, user.UpdatedAt,
            context.UserBlacklist.Where(entry => entry.UserId == user.Id).Select(entry => entry.Reason).FirstOrDefault(),
            user.Role == UserRole.CUSTOMER && context.UserBlacklist.Any(entry => entry.UserId != user.Id &&
                (entry.Email == user.Email.Trim().ToLower() ||
                 (entry.PhoneKey != null && entry.PhoneKey == CarRentalDbContext.BlacklistPhoneKey(user.Phone))))));

    public sealed record UserView(long Id, string FullName, string Email, string Phone,
        string Role, string Status, DateTime CreatedAt, DateTime UpdatedAt,
        string? BlockReason, bool ContactBlocked);

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct,
        [FromQuery, Range(1, 1000000)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 20,
        [FromQuery, StringLength(100)] string? search = null)
    {
        var users = context.Users.AsNoTracking();
        var term = search?.Trim();
        if (!string.IsNullOrEmpty(term))
            users = users.Where(user => user.FullName.Contains(term) ||
                user.Email.Contains(term) || user.Phone.Contains(term));
        var totalCount = await users.CountAsync(ct);
        var items = await Project(users.OrderByDescending(user => user.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)).ToListAsync(ct);
        return Ok(new { items, totalCount, page, pageSize });
    }

    [HttpGet("blacklist")]
    public async Task<IActionResult> Blacklist(CancellationToken ct,
        [FromQuery, Range(1, 1000000)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 20,
        [FromQuery, StringLength(100)] string? search = null)
    {
        var entries = context.UserBlacklist.AsNoTracking();
        var term = search?.Trim();
        if (!string.IsNullOrEmpty(term))
            entries = entries.Where(entry => entry.User.FullName.Contains(term) ||
                entry.Email.Contains(term) || entry.Phone.Contains(term) || entry.Reason.Contains(term));

        var totalCount = await entries.CountAsync(ct);
        var items = await entries.OrderByDescending(entry => entry.BlockedAt)
            .ThenByDescending(entry => entry.UserId)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(entry => new
            {
                Id = entry.UserId,
                entry.User.FullName,
                entry.Email,
                entry.Phone,
                entry.Reason,
                entry.BlockedAt,
                BlockedByName = entry.BlockedBy == null ? null : entry.BlockedBy.FullName
            })
            .ToListAsync(ct);
        return Ok(new { items, totalCount, page, pageSize });
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Detail(long id, CancellationToken ct)
    {
        var user = await Project(context.Users.AsNoTracking().Where(user => user.Id == id))
            .SingleOrDefaultAsync(ct);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPut("{id:long}/status")]
    public async Task<IActionResult> UpdateStatus(long id, StatusRequest input, CancellationToken ct)
    {
        if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actor))
            return Unauthorized();
        var blocked = input.Blocked == true;
        var reason = input.Reason?.Trim();
        if (blocked && string.IsNullOrEmpty(reason))
            return BadRequest(new { message = "Vui lòng nhập lý do chặn." });

        await using var transaction = await context.BeginUserIdentityTransactionAsync(ct);
        var version = User.FindFirstValue("session_version");
        var admin = await context.Users.AsNoTracking().SingleOrDefaultAsync(user =>
            user.Id == actor && user.Role == UserRole.ADMIN && user.Status == UserStatus.ACTIVE, ct);
        if (admin is null || admin.SessionVersion?.ToString("D") != version) return Forbid();

        var user = await context.Users.SingleOrDefaultAsync(user => user.Id == id, ct);
        if (user is null) return NotFound();
        if (blocked && user.Role == UserRole.ADMIN)
            return Conflict(new { message = "Không thể chặn tài khoản quản trị viên." });

        var entry = await context.UserBlacklist.SingleOrDefaultAsync(item => item.UserId == id, ct);
        var status = blocked ? UserStatus.LOCKED : UserStatus.ACTIVE;
        if (user.Status != status || blocked != (entry is not null))
        {
            var now = DateTime.UtcNow;
            if (blocked && entry is null)
            {
                var email = user.Email.Trim().ToLowerInvariant();
                var phone = user.Phone;
                context.UserBlacklist.Add(new UserBlacklistEntry
                {
                    UserId = id,
                    Email = email,
                    Phone = phone,
                    Reason = reason!,
                    BlockedAt = now,
                    BlockedByUserId = actor
                });
                var newSessionVersion = Guid.NewGuid();
                await context.Users.Where(other => other.Id != id && other.Role == UserRole.CUSTOMER &&
                    (other.Email.Trim().ToLower() == email ||
                     (CarRentalDbContext.BlacklistPhoneKey(other.Phone) != null &&
                      CarRentalDbContext.BlacklistPhoneKey(other.Phone) == CarRentalDbContext.BlacklistPhoneKey(phone))))
                    .ExecuteUpdateAsync(setters => setters.SetProperty(other => other.SessionVersion, newSessionVersion), ct);
            }
            else if (!blocked && entry is not null)
            {
                context.UserBlacklist.Remove(entry);
            }

            user.Status = status;
            user.SessionVersion = Guid.NewGuid();
            user.UpdatedAt = now;
            await context.SaveChangesAsync(ct);
        }

        var result = await Project(context.Users.AsNoTracking().Where(item => item.Id == id)).SingleAsync(ct);
        await transaction.CommitAsync(ct);
        return Ok(result);
    }
}
