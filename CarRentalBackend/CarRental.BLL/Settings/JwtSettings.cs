
namespace CarRental.BLL.Settings
{
    // Các cấu hình dùng để tạo JWT.
    public class JwtSettings
    {
        public string Key { get; set; } = string.Empty;

        public string Issuer { get; set; } = string.Empty;

        public string Audience { get; set; } = string.Empty;

        public int ExpireMinutes { get; set; } = 60;
    }
}
