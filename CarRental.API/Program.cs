using CarRental.BLL.Interfaces;
using CarRental.BLL.Services;
using CarRental.BLL.Settings;
using CarRental.DAL.Context;
using CarRental.DAL.Interfaces;
using CarRental.DAL.Repositories;
using CarRental.Models.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var webRootPath = Path.GetFullPath(
    builder.Environment.WebRootPath ?? "wwwroot", builder.Environment.ContentRootPath);
Directory.CreateDirectory(webRootPath);

if (builder.Environment.WebRootFileProvider is Microsoft.Extensions.FileProviders.NullFileProvider)
{
    builder.Environment.WebRootPath = webRootPath;
    builder.Environment.WebRootFileProvider =
        new Microsoft.Extensions.FileProviders.PhysicalFileProvider(webRootPath);
}

// 1. Đăng ký Controllers cho Web API.
builder.Services.AddControllers();

// 2. Đọc chuỗi kết nối từ appsettings.json.
var connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection");

// 3. Kiểm tra chuỗi kết nối có tồn tại.
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Chưa cấu hình DefaultConnection trong appsettings.json.");
}

// 4. Đăng ký DbContext với Dependency Injection.
// Scoped: mỗi HTTP request sử dụng một instance Repository.
builder.Services.AddScoped<IUserRepository, UserRepository>();
// Đăng ký dịch vụ xử lý nghiệp vụ xác thực.
builder.Services.AddScoped<IAuthService, AuthService>();
// Đăng ký dịch vụ băm và kiểm tra mật khẩu.
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

// Vehicle Model Repository
builder.Services.AddScoped<IVehicleModelRepository, VehicleModelRepository>();

// Vehicle Model Service
builder.Services.AddScoped<IVehicleModelService, VehicleModelService>();

// Vehicle Management
builder.Services.AddScoped<IVehicleRepository, VehicleRepository>();
builder.Services.AddScoped<IVehicleService, VehicleService>();

// Rental Orders
builder.Services.AddScoped<IRentalOrderRepository, RentalOrderRepository>();
builder.Services.AddScoped<IRentalOrderService, RentalOrderService>();

// Payments
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

// Vehicle Images
builder.Services.AddScoped<IVehicleImageRepository, VehicleImageRepository>();
builder.Services.AddScoped<IVehicleImageService>(services => new VehicleImageService(
    services.GetRequiredService<IVehicleImageRepository>(),
    webRootPath,
    services.GetRequiredService<ILogger<VehicleImageService>>()));

// EF Core sử dụng SQL Server làm hệ quản trị cơ sở dữ liệu.
builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("JwtSettings"));

builder.Services.AddScoped<JwtTokenService>();

var jwtSettings = builder.Configuration
    .GetSection("JwtSettings")
    .Get<JwtSettings>()
    ?? throw new InvalidOperationException(
        "Thiếu cấu hình JWT.");

if (string.IsNullOrWhiteSpace(jwtSettings.Key) ||
    Encoding.UTF8.GetByteCount(jwtSettings.Key) < 32 ||
    string.IsNullOrWhiteSpace(jwtSettings.Issuer) ||
    string.IsNullOrWhiteSpace(jwtSettings.Audience) ||
    jwtSettings.ExpireMinutes <= 0)
{
    throw new InvalidOperationException(
        "Cấu hình JWT không hợp lệ.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async validated =>
            {
                var claims = validated.Principal;
                var tokenVersion = claims?.FindFirstValue("session_version");
                if (!long.TryParse(claims?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ||
                    !Enum.TryParse<CarRental.Models.Enums.UserRole>(claims?.FindFirstValue(ClaimTypes.Role), out var role))
                {
                    validated.Fail("Invalid account claims.");
                    return;
                }

                var database = validated.HttpContext.RequestServices.GetRequiredService<CarRentalDbContext>();
                var active = await database.Users.AsNoTracking()
                    .Where(user => user.Id == id && user.Role == role &&
                        user.Status == CarRental.Models.Enums.UserStatus.ACTIVE &&
                        (user.Role != CarRental.Models.Enums.UserRole.CUSTOMER ||
                         !database.UserBlacklist.Any(entry => entry.Email == user.Email.Trim().ToLower() ||
                             (entry.PhoneKey != null &&
                              entry.PhoneKey == CarRentalDbContext.BlacklistPhoneKey(user.Phone)))))
                    .Select(user => new { user.SessionVersion })
                    .SingleOrDefaultAsync(validated.HttpContext.RequestAborted);
                if (active is null || active.SessionVersion?.ToString("D") != tokenVersion)
                    validated.Fail("Account or session is inactive.");
            }
        };

        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtSettings.Issuer,

                ValidateAudience = true,
                ValidAudience = jwtSettings.Audience,

                ValidateLifetime = true,
                RequireExpirationTime = true,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSettings.Key)),

                NameClaimType = ClaimTypes.Name,
                RoleClaimType = ClaimTypes.Role,

                ClockSkew = TimeSpan.FromSeconds(30)
            };
    });

builder.Services.AddAuthorization();

builder.Services.AddDbContext<CarRentalDbContext>(options =>
    options.UseSqlServer(connectionString));


// 5. Tạo ứng dụng.
var app = builder.Build();
// 6. Chuyển hướng HTTP sang HTTPS.
app.UseHttpsRedirection();

var vehicleUploadsPath = Path.Combine(webRootPath, "uploads", "vehicles");
Directory.CreateDirectory(vehicleUploadsPath);
using var vehicleUploadsProvider =
    new Microsoft.Extensions.FileProviders.PhysicalFileProvider(vehicleUploadsPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = vehicleUploadsProvider,
    RequestPath = "/uploads/vehicles"
});

app.UseStaticFiles();

// Xác thực JWT Access Token.
app.UseAuthentication();

// Kiểm tra quyền truy cập.
app.UseAuthorization();

app.MapControllers();

// 9. Chạy Web API.
app.Run();
