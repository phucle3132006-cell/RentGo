
using CarRental.BLL.Interfaces;
using CarRental.DAL.Interfaces;
using CarRental.Models.DTOs.Auth;
using CarRental.Models.Entities;
using CarRental.Models.Enums;
using Microsoft.AspNetCore.Identity;

namespace CarRental.BLL.Services
{
    // Xử lý nghiệp vụ xác thực tài khoản.
    public class AuthService : IAuthService
    {
        private readonly JwtTokenService _jwtTokenService;
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher<User> _passwordHasher;

        public AuthService(
            IUserRepository userRepository,
            IPasswordHasher<User> passwordHasher,
            JwtTokenService jwtTokenService)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _jwtTokenService = jwtTokenService;
        }

        // Đăng ký tài khoản mới với vai trò CUSTOMER.
        public async Task<RegisterResponse> RegisterAsync(
            RegisterRequest request)
        {
            // Chuẩn hóa dữ liệu đầu vào.
            string fullName = request.FullName.Trim();
            string email = request.Email.Trim().ToLowerInvariant();
            string phone = request.Phone.Trim();

            // Kiểm tra các trường bắt buộc.
            if (string.IsNullOrWhiteSpace(fullName) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(phone) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                throw new ArgumentException(
                    "Vui lòng nhập đầy đủ thông tin.");
            }

            // Kiểm tra độ dài mật khẩu.
            if (request.Password.Length < 8)
            {
                throw new ArgumentException(
                    "Mật khẩu phải có ít nhất 8 ký tự.");
            }

            // Kiểm tra xác nhận mật khẩu.
            if (request.Password != request.ConfirmPassword)
            {
                throw new ArgumentException(
                    "Mật khẩu xác nhận không khớp.");
            }

            // Kiểm tra email đã tồn tại.
            if (await _userRepository.EmailExistsAsync(email))
            {
                throw new InvalidOperationException(
                    "Email đã được sử dụng.");
            }

            // Kiểm tra số điện thoại đã tồn tại.
            if (await _userRepository.PhoneExistsAsync(phone))
            {
                throw new InvalidOperationException(
                    "Số điện thoại đã được sử dụng.");
            }

            // Tạo đối tượng User.
            var user = new User
            {
                FullName = fullName,
                Email = email,
                Phone = phone,

                // Không cho người đăng ký tự chọn ADMIN.
                Role = UserRole.CUSTOMER,

                // Tài khoản mới có trạng thái ACTIVE.
                Status = UserStatus.ACTIVE
            };

            // Băm mật khẩu trước khi lưu vào database.
            user.PasswordHash =
                _passwordHasher.HashPassword(
                    user,
                    request.Password);

            // Thêm User vào DbContext.
            await _userRepository.AddAsync(user);

            // Lưu xuống SQL Server.
            await _userRepository.SaveChangesAsync();

            // Trả thông tin đăng ký, không trả mật khẩu.
            return new RegisterResponse
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role.ToString(),
                Message = "Đăng ký tài khoản thành công."
            };
        }

        public async Task<LoginResponse?> LoginAsync(
            LoginRequest request)
        {
            // Chuẩn hóa email.
            string email = request.Email.Trim().ToLowerInvariant();

            // Tìm tài khoản theo email.
            var user = await _userRepository.GetByEmailAsync(email);

            // Không tìm thấy tài khoản.
            if (user == null)
            {
                return null;
            }

            // Kiểm tra mật khẩu đã băm.
            var passwordResult =
                _passwordHasher.VerifyHashedPassword(
                    user,
                    user.PasswordHash,
                    request.Password);

            if (passwordResult ==
                PasswordVerificationResult.Failed)
            {
                return null;
            }

            // Không cho tài khoản bị khóa đăng nhập.
            if (user.Status != UserStatus.ACTIVE)
            {
                return null;
            }

            // Tạo JWT cho tài khoản hợp lệ.
            var (token, expiresAt) =
                _jwtTokenService.GenerateToken(user);

            return new LoginResponse
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role.ToString(),
                AccessToken = token,
                ExpiresAt = expiresAt
            };
        }

    }
}
