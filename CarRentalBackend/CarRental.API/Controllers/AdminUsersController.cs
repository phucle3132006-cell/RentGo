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
    public sealed record StatusRequest([Required] bool? Blocked);

    private static IQueryable<UserView> Project(IQueryable<User> users) =>
        users.Select(user => new UserView(user.Id, user.FullName, user.Email, user.Phone,
            user.Role.ToString(), user.Status.ToString(), user.CreatedAt, user.UpdatedAt));

    public sealed record UserView(long Id, string FullName, string Email, string Phone,
        string Role, string Status, DateTime CreatedAt, DateTime UpdatedAt);

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
        if (input.Blocked == true && actor == id)
            return Conflict(new { message = "Bạn không thể tự chặn tài khoản của mình." });

        await using var transaction = await context.Database.BeginTransactionAsync(ct);
        await context.Database.ExecuteSqlRawAsync("""
            DECLARE @result int;
            EXEC @result = sys.sp_getapplock @Resource = N'RentGo.AdminUsers.Status',
                @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 5000;
            IF @result < 0 THROW 51001, 'Could not lock user status changes.', 1;
            """, ct);

        var version = User.FindFirstValue("session_version");
        var admin = await context.Users.AsNoTracking().SingleOrDefaultAsync(user =>
            user.Id == actor && user.Role == UserRole.ADMIN && user.Status == UserStatus.ACTIVE, ct);
        if (admin is null || admin.SessionVersion?.ToString("D") != version) return Forbid();

        var user = await context.Users.SingleOrDefaultAsync(user => user.Id == id, ct);
        if (user is null) return NotFound();
        var status = input.Blocked == true ? UserStatus.LOCKED : UserStatus.ACTIVE;
        if (user.Status != status)
        {
            if (status == UserStatus.LOCKED && user.Role == UserRole.ADMIN &&
                !await context.Users.AnyAsync(other => other.Id != id &&
                    other.Role == UserRole.ADMIN && other.Status == UserStatus.ACTIVE, ct))
                return Conflict(new { message = "Phải giữ ít nhất một quản trị viên đang hoạt động." });

            user.Status = status;
            user.SessionVersion = Guid.NewGuid();
            user.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(ct);
        }

        var result = await Project(context.Users.AsNoTracking().Where(item => item.Id == id)).SingleAsync(ct);
        await transaction.CommitAsync(ct);
        return Ok(result);
    }
}
