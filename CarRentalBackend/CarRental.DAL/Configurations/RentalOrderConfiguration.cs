using CarRental.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarRental.DAL.Configurations
{
    // Ánh xạ Entity RentalOrder
    // với bảng dbo.rental_orders trong SQL Server.
    public class RentalOrderConfiguration
        : IEntityTypeConfiguration<RentalOrder>
    {
        public void Configure(
            EntityTypeBuilder<RentalOrder> builder)
        {
            // 1. Tên bảng và schema.
            builder.ToTable("rental_orders", "dbo");

            // 2. Khóa chính.
            builder.HasKey(ro => ro.Id);

            builder.Property(ro => ro.Id)
                   .HasColumnName("id")
                   .ValueGeneratedOnAdd();

            // 3. Mã đơn thuê.
            builder.Property(ro => ro.OrderCode)
                   .HasColumnName("order_code")
                   .HasMaxLength(30)
                   .IsUnicode(false)
                   .IsRequired();

            // 4. Khóa ngoại đến bảng users.
            builder.Property(ro => ro.CustomerId)
                   .HasColumnName("customer_id")
                   .IsRequired();

            // 5. Thời gian bắt đầu thuê.
            builder.Property(ro => ro.StartDatetime)
                   .HasColumnName("start_datetime")
                   .HasColumnType("datetime2")
                   .IsRequired();

            // 6. Thời gian kết thúc thuê.
            builder.Property(ro => ro.EndDatetime)
                   .HasColumnName("end_datetime")
                   .HasColumnType("datetime2")
                   .IsRequired();

            // 7. Tổng tiền đơn thuê.
            builder.Property(ro => ro.TotalAmount)
                   .HasColumnName("total_amount")
                   .HasColumnType("decimal(10,2)")
                   .IsRequired();

            // 8. Trạng thái đơn thuê.
            // PENDING, CONFIRMED, PAID,
            // RENTING, COMPLETED, CANCELLED.
            builder.Property(ro => ro.Status)
                   .HasColumnName("status")
                   .HasConversion<string>()
                   .HasMaxLength(30)
                   .IsUnicode(false)
                   .IsRequired();

            builder.Property(ro => ro.ReviewedByUserId).HasColumnName("reviewed_by_user_id");
            builder.Property(ro => ro.ReviewedAt).HasColumnName("reviewed_at");
            builder.Property(ro => ro.RejectionReason).HasColumnName("rejection_reason").HasMaxLength(500);
            builder.HasOne<User>().WithMany().HasForeignKey(ro => ro.ReviewedByUserId)
                   .OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_rental_orders_reviewed_by_user");

            // 9. Ghi chú.
            // Cho phép NULL trong SQL Server.
            builder.Property(ro => ro.Note)
                   .HasColumnName("note")
                   .HasColumnType("nvarchar(max)")
                   .IsRequired(false);

            // 10. Ngày tạo.
            builder.Property(ro => ro.CreatedAt)
                   .HasColumnName("created_at")
                   .HasColumnType("datetime2")
                   .HasDefaultValueSql("SYSDATETIME()")
                   .IsRequired();

            // 11. Ngày cập nhật.
            builder.Property(ro => ro.UpdatedAt)
                   .HasColumnName("updated_at")
                   .HasColumnType("datetime2")
                   .HasDefaultValueSql("SYSDATETIME()")
                   .IsRequired();

            // 12. Mỗi mã đơn thuê phải duy nhất.
            builder.HasIndex(ro => ro.OrderCode)
                   .IsUnique()
                   .HasDatabaseName("UQ_rental_orders_order_code");

            // 13. Quan hệ N - 1:
            // Nhiều đơn thuê thuộc một khách hàng.
            builder.HasOne(ro => ro.Customer)
                   .WithMany(u => u.RentalOrders)
                   .HasForeignKey(ro => ro.CustomerId)
                   .OnDelete(DeleteBehavior.NoAction);

            // 14. Quan hệ 1 - N:
            // Một đơn thuê có nhiều chi tiết đơn thuê.
            builder.HasMany(ro => ro.RentalOrderDetails)
                   .WithOne(rod => rod.RentalOrder)
                   .HasForeignKey(rod => rod.RentalOrderId)
                   .OnDelete(DeleteBehavior.NoAction);

            // 15. Quan hệ 1 - N:
            // Một đơn thuê có thể có nhiều giao dịch thanh toán.
            builder.HasMany(ro => ro.Payments)
                   .WithOne(p => p.RentalOrder)
                   .HasForeignKey(p => p.RentalOrderId)
                   .OnDelete(DeleteBehavior.NoAction);
        }
    }
}