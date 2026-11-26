using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CarRental.API.Controllers
{
    [ApiController]
    [Route("api/auth-test")]
    public class AuthTestController : ControllerBase
    {
        // Chỉ người dùng đã đăng nhập mới truy cập được.
        [Authorize]
        [HttpGet("me")]
        public IActionResult Me()
        {
            return Ok(new
            {
                Id = User.FindFirstValue(ClaimTypes.NameIdentifier),
                Name = User.FindFirstValue(ClaimTypes.Name),
                Role = User.FindFirstValue(ClaimTypes.Role)
            });
        }

        // Chỉ ADMIN mới truy cập được.
        [Authorize(Roles = "ADMIN")]
        [HttpGet("admin")]
        public IActionResult AdminOnly()
        {
            return Ok(new
            {
                Message = "Bạn có quyền ADMIN."
            });
        }
    }
}