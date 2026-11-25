using CarRental.Models.Entities;
using Microsoft.EntityFrameworkCore;

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

        // 7 bảng nghiệp vụ trong database.
        public DbSet<User> Users { get; set; } = null!;

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