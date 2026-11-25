
namespace CarRental.Models.DTOs.Auth
{
    // Kết quả đăng nhập thành công.
    public class LoginResponse
    {
        public long Id { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;

        public string AccessToken { get; set; } = string.Empty;

        public DateTime ExpiresAt { get; set; }
    }
}
