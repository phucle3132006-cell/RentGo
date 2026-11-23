namespace CarRental.Models.DTOs.Auth
{
    // Thông tin trả về sau khi đăng ký thành công.
    public class RegisterResponse
    {
        public long Id { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Role { get; set; } = "CUSTOMER";

        public string Message { get; set; } =
            "Đăng ký tài khoản thành công.";
    }
}