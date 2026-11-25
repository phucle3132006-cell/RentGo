using CarRental.DAL.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarRental.API.Controllers
{
    [ApiController]
    [Route("api/database-test")]
    public class DatabaseTestController : ControllerBase
    {
        private readonly CarRentalDbContext _context;

        // DbContext được Dependency Injection cung cấp.
        public DatabaseTestController(CarRentalDbContext context)
        {
            _context = context;
        }

        // GET: /api/database-test
        [HttpGet]
        public async Task<IActionResult> TestConnection()
        {
            try
            {
                // Kiểm tra SQL Server có thể kết nối hay không.
                bool canConnect =
                    await _context.Database.CanConnectAsync();

                if (!canConnect)
                {
                    return StatusCode(503, new
                    {
                        success = false,
                        message = "Không thể kết nối SQL Server."
                    });
                }

                // Kiểm tra truy vấn thực tế đến bảng users.
                int userCount = await _context.Users.CountAsync();

                return Ok(new
                {
                    success = true,
                    message = "Kết nối database thành công!",
                    database = "CarMotorbikeRentalDB",
                    totalUsers = userCount
                });
            }
            catch (Exception )
            {
                // Không trả chi tiết lỗi nội bộ cho client.
                // Trong dự án thực tế nên ghi lỗi vào ILogger.
                return StatusCode(500, new
                {
                    success = false,
                    message = "Có lỗi khi truy vấn database."
                });
            }
        }
    }
}