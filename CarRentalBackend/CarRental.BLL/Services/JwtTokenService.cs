
using CarRental.BLL.Settings;
using CarRental.Models.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CarRental.BLL.Services
{
    // Dịch vụ tạo JWT Access Token.
    public class JwtTokenService
    {
        private readonly JwtSettings _settings;

        public JwtTokenService(
            IOptions<JwtSettings> options)
        {
            _settings = options.Value;
        }

        public (string Token, DateTime ExpiresAt)
            GenerateToken(User user)
        {
            if (string.IsNullOrWhiteSpace(_settings.Key) ||
                Encoding.UTF8.GetByteCount(_settings.Key) < 32)
            {
                throw new InvalidOperationException(
                    "JWT Secret Key chưa được cấu hình hợp lệ.");
            }

            // Thông tin người dùng được lưu trong Token.
            var claims = new List<Claim>
            {
                new Claim(
                    JwtRegisteredClaimNames.Sub,
                    user.Id.ToString()),

                new Claim(
                    JwtRegisteredClaimNames.Email,
                    user.Email),

                new Claim(
                    ClaimTypes.NameIdentifier,
                    user.Id.ToString()),

                new Claim(
                    ClaimTypes.Name,
                    user.FullName),

                new Claim(
                    ClaimTypes.Role,
                    user.Role.ToString()),

                new Claim(
                    JwtRegisteredClaimNames.Jti,
                    Guid.NewGuid().ToString())
            };

            if (user.SessionVersion is Guid version)
                claims.Add(new Claim("session_version", version.ToString("D")));

            // Tạo khóa ký Token.
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_settings.Key));

            var credentials = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256);

            var expiresAt = DateTime.UtcNow.AddMinutes(
                _settings.ExpireMinutes);

            // Tạo JWT.
            var token = new JwtSecurityToken(
                issuer: _settings.Issuer,
                audience: _settings.Audience,
                claims: claims,
                expires: expiresAt,
                signingCredentials: credentials);

            return (
                new JwtSecurityTokenHandler()
                    .WriteToken(token),
                expiresAt
            );
        }
    }
}
