
using CarRental.Models.DTOs.Auth;

namespace CarRental.BLL.Interfaces
{
    // Các chức năng xác thực tài khoản.
    public interface IAuthService
    {
        // Đăng ký tài khoản.
        Task<RegisterResponse> RegisterAsync(
            RegisterRequest request);

        // Đăng nhập và nhận JWT.
        Task<LoginResponse?> LoginAsync(
            LoginRequest request);
    }
}
