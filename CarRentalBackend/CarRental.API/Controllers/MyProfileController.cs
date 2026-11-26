using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using CarRental.DAL.Context;
using CarRental.Models.Entities;
using CarRental.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CarRental.API.Controllers;

[ApiController]
[Authorize(Roles = "CUSTOMER")]
[Route("api/profile/me")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class MyProfileController(
    CarRentalDbContext context, IPasswordHasher<User> passwordHasher) : ControllerBase
{
    public sealed record ProfileUpdate(
        [Required, StringLength(100)] string FullName,
        [Required, StringLength(20)] string Phone);

    public sealed record PasswordChange(
        [Required, StringLength(1024)] string CurrentPassword,
        [Required, StringLength(128, MinimumLength = 8)] string NewPassword,
        [Required, StringLength(128)] string ConfirmPassword);

    private IQueryable<User> Account()
    {
        var id = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var version = User.FindFirstValue("session_version");
        Guid? sessionVersion = version is null ? null : Guid.Parse(version);
        return context.Users.Where(user => user.Id == id && user.Role == UserRole.CUSTOMER &&
            user.Status == UserStatus.ACTIVE && user.SessionVersion == sessionVersion);
    }

    private static object Profile(User user) => new { user.Id, user.FullName, user.Email, user.Phone };

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var user = await Account().AsNoTracking().SingleOrDefaultAsync(ct);
        return user is null ? Unauthorized() : Ok(Profile(user));
    }

    [HttpPut]
    public async Task<IActionResult> Update(ProfileUpdate input, CancellationToken ct)
    {
        var fullName = input.FullName.Trim();
        var phone = input.Phone.Trim();
        await using var transaction = await context.BeginUserIdentityTransactionAsync(ct);
        var account = Account();
        var user = await account.AsNoTracking().SingleOrDefaultAsync(ct);
        if (user is null) return Unauthorized();

        var digits = phone.StartsWith('+') ? phone[1..] : phone;
        if (fullName.Length == 0 || fullName.Any(char.IsControl) ||
            (phone != user.Phone && (digits.Length is < 8 or > 15 || !digits.All(char.IsAsciiDigit))))
            return BadRequest(new { message = "Họ tên hoặc số điện thoại không hợp lệ." });

        if (await context.IsBlacklistedAsync(user.Email, phone, ct))
            return Conflict(new { message = "Số điện thoại đang nằm trong danh sách chặn." });

        try
        {
            var updated = await account.ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.FullName, fullName)
                .SetProperty(item => item.Phone, phone)
                .SetProperty(item => item.UpdatedAt, DateTime.UtcNow), ct);
            if (updated == 0) return Unauthorized();
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            return Conflict(new { message = "Số điện thoại đã được sử dụng." });
        }

        await transaction.CommitAsync(ct);
        user.FullName = fullName;
        user.Phone = phone;
        return Ok(Profile(user));
    }

    [HttpPut("password")]
    public async Task<IActionResult> ChangePassword(PasswordChange input, CancellationToken ct)
    {
        if (input.NewPassword != input.ConfirmPassword)
            return BadRequest(new { message = "Mật khẩu nhập lại không khớp." });

        if (input.CurrentPassword == input.NewPassword || string.IsNullOrWhiteSpace(input.NewPassword))
            return BadRequest(new { message = "Mật khẩu mới phải khác mật khẩu hiện tại và có ít nhất 8 ký tự." });

        var account = Account();
        var user = await account.AsNoTracking().SingleOrDefaultAsync(ct);
        if (user is null) return Unauthorized();
        if (passwordHasher.VerifyHashedPassword(user, user.PasswordHash, input.CurrentPassword) ==
            PasswordVerificationResult.Failed)
            return BadRequest(new { message = "Mật khẩu hiện tại không đúng." });

        var hash = passwordHasher.HashPassword(user, input.NewPassword);
        var nextVersion = Guid.NewGuid();
        var updated = await account.Where(item => item.PasswordHash == user.PasswordHash)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.PasswordHash, hash)
                .SetProperty(item => item.SessionVersion, nextVersion)
                .SetProperty(item => item.UpdatedAt, DateTime.UtcNow), ct);
        return updated == 0 ? Unauthorized() : NoContent();
    }
}
