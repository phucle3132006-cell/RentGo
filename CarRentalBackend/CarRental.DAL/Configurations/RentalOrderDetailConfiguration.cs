using CarRental.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarRental.DAL.Configurations
{
    // Cấu hình ánh xạ Entity RentalOrderDetail
    // với bảng dbo.rental_order_details.
    public class RentalOrderDetailConfiguration
        : IEntityTypeConfiguration<RentalOrderDetail>
    {
        public void Configure(
            EntityTypeBuilder<RentalOrderDetail> builder)
        {
            // 1. Xác định bảng SQL Server.
            builder.ToTable("rental_order_details", "dbo");

            // 2. Khóa chính.
            builder.HasKey(rod => rod.Id);

            builder.Property(rod => rod.Id)
                   .HasColumnName("id")
                   .ValueGeneratedOnAdd();

            // 3. Khóa ngoại đến bảng rental_orders.
            builder.Property(rod => rod.RentalOrderId)
                   .HasColumnName("rental_order_id")
                   .IsRequired();

            // 4. Khóa ngoại đến bảng vehicles.
            builder.Property(rod => rod.VehicleId)
                   .HasColumnName("vehicle_id")
                   .IsRequired();

            // 5. Giá thuê mỗi ngày tại thời điểm đặt xe.
            builder.Property(rod => rod.PricePerDay)
                   .HasColumnName("price_per_day")
                   .HasColumnType("decimal(10,2)")
                   .IsRequired();

            // 6. Số ngày thuê.
            builder.Property(rod => rod.RentalDays)
                   .HasColumnName("rental_days")
                   .IsRequired();

            // 7. Thành tiền của một xe.
            builder.Property(rod => rod.Subtotal)
                   .HasColumnName("subtotal")
                   .HasColumnType("decimal(10,2)")
                   .IsRequired();

            // 8. Trạng thái chi tiết đơn thuê.
            // RESERVED, RENTING, RETURNED, CANCELLED.
            builder.Property(rod => rod.Status)
                   .HasColumnName("status")
                   .HasConversion<string>()
                   .HasMaxLength(30)
                   .IsUnicode(false)
                   .IsRequired();

            // 9. Ngày tạo chi tiết đơn thuê.
            builder.Property(rod => rod.CreatedAt)
                   .HasColumnName("created_at")
                   .HasColumnType("datetime2")
                   .HasDefaultValueSql("SYSDATETIME()")
                   .IsRequired();

            // 10. Ràng buộc UNIQUE:
            // Một xe không được xuất hiện hai lần
            // trong cùng một đơn thuê.
            builder.HasIndex(rod => new
            {
                rod.RentalOrderId,
                rod.VehicleId
            })
            .IsUnique()
            .HasDatabaseName(
                "UQ_rental_order_details_order_vehicle"
            );

            // 11. Quan hệ N - 1:
            // Nhiều chi tiết thuộc một đơn thuê.
            builder.HasOne(rod => rod.RentalOrder)
                   .WithMany(ro => ro.RentalOrderDetails)
                   .HasForeignKey(rod => rod.RentalOrderId)
                   .OnDelete(DeleteBehavior.NoAction);

            // 12. Quan hệ N - 1:
            // Một xe có thể xuất hiện trong nhiều
            // chi tiết đơn thuê theo thời gian.
            builder.HasOne(rod => rod.Vehicle)
                   .WithMany(v => v.RentalOrderDetails)
                   .HasForeignKey(rod => rod.VehicleId)
                   .OnDelete(DeleteBehavior.NoAction);
        }
    }
}