using System.ComponentModel.DataAnnotations;

namespace CarRental.Models.DTOs.Auth
{
    // Dữ liệu khách hàng gửi lên khi đăng ký tài khoản.
    public class RegisterRequest
    {
        // Họ tên không được để trống.
        [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        // Email phải đúng định dạng.
        [Required(ErrorMessage = "Vui lòng nhập email.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        // Số điện thoại không được để trống.
        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [StringLength(20)]
        public string Phone { get; set; } = string.Empty;

        // Mật khẩu tối thiểu 8 ký tự.
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        [MinLength(8, ErrorMessage = "Mật khẩu phải có ít nhất 8 ký tự.")]
        public string Password { get; set; } = string.Empty;

        // Xác nhận lại mật khẩu.
        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu.")]
        [Compare(nameof(Password),
            ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}