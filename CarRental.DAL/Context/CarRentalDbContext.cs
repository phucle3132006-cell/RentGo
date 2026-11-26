using CarRental.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace CarRental.DAL.Context
{
    // DbContext đại diện cho phiên làm việc
    // giữa ứng dụng RentGo và SQL Server.
    public class CarRentalDbContext : DbContext
    {
        // Nhận cấu hình kết nối từ Dependency Injection.
        public CarRentalDbContext(
            DbContextOptions<CarRentalDbContext> options
        ) : base(options)
        {
        }

        // Các bảng nghiệp vụ trong database.
        public DbSet<User> Users { get; set; } = null!;

        public DbSet<UserBlacklistEntry> UserBlacklist { get; set; } = null!;

        [DbFunction("rentgo_blacklist_phone", "dbo")]
        public static string? BlacklistPhoneKey(string phone) => throw new NotSupportedException();

        public Task<bool> IsBlacklistedAsync(string email, string phone, CancellationToken ct = default) =>
            UserBlacklist.AnyAsync(entry => entry.Email == email.Trim().ToLower() ||
                (entry.PhoneKey != null && entry.PhoneKey == BlacklistPhoneKey(phone)), ct);

        public async Task<IDbContextTransaction> BeginUserIdentityTransactionAsync(CancellationToken ct = default)
        {
            var transaction = await Database.BeginTransactionAsync(ct);
            try
            {
                // Serialize account blocking, registration, and profile contact changes.
                await Database.ExecuteSqlRawAsync("""
                    DECLARE @result int;
                    EXEC @result = sys.sp_getapplock @Resource = N'RentGo.AdminUsers.Status',
                        @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 5000;
                    IF @result < 0 THROW 51001, 'Could not lock user status changes.', 1;
                    """, ct);
                return transaction;
            }
            catch
            {
                await transaction.DisposeAsync();
                throw;
            }
        }

        public DbSet<VehicleModel> VehicleModels { get; set; } = null!;

        public DbSet<Vehicle> Vehicles { get; set; } = null!;

        public DbSet<VehicleImage> VehicleImages { get; set; } = null!;

        public DbSet<RentalOrder> RentalOrders { get; set; } = null!;

        public DbSet<RentalOrderDetail> RentalOrderDetails { get; set; } = null!;

        public DbSet<Payment> Payments { get; set; } = null!;

        // Đăng ký Fluent API khi EF Core xây dựng model.
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<TransferRequest>(entity =>
            {
                entity.ToTable("TransferRequests", "dbo");
                entity.HasOne(item => item.Order).WithMany().HasForeignKey(item => item.OrderId);
                entity.Property(item => item.PaymentMethod).HasMaxLength(16).IsUnicode(false);
                entity.Property(item => item.Status).HasMaxLength(16);
                entity.Property(item => item.Reason).HasMaxLength(500);
            });

            // Tự động tìm và áp dụng các class
            // IEntityTypeConfiguration<T> trong DAL.
            modelBuilder.ApplyConfigurationsFromAssembly(
                typeof(CarRentalDbContext).Assembly
            );
        }
    }
}
