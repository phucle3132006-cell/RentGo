using CarRental.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarRental.DAL.Configurations
{
    // Cấu hình ánh xạ Entity Payment
    // với bảng dbo.payments trong SQL Server.
    public class PaymentConfiguration
        : IEntityTypeConfiguration<Payment>
    {
        public void Configure(EntityTypeBuilder<Payment> builder)
        {
            // 1. Xác định tên bảng và schema.
            builder.ToTable("payments", "dbo");

            // 2. Khóa chính.
            builder.HasKey(p => p.Id);

            builder.Property(p => p.Id)
                   .HasColumnName("id")
                   .ValueGeneratedOnAdd();

            // 3. Khóa ngoại đến bảng rental_orders.
            builder.Property(p => p.RentalOrderId)
                   .HasColumnName("rental_order_id")
                   .IsRequired();

            // 4. Số tiền thanh toán.
            builder.Property(p => p.Amount)
                   .HasColumnName("amount")
                   .HasColumnType("decimal(12,2)")
                   .IsRequired();

            // 5. Phương thức thanh toán.
            // CASH, BANK_TRANSFER, MOMO,
            // VNPAY, CARD, OTHER.
            builder.Property(p => p.PaymentMethod)
                   .HasColumnName("payment_method")
                   .HasConversion<string>()
                   .HasMaxLength(30)
                   .IsUnicode(false)
                   .IsRequired();

            // 6. Mã giao dịch.
            builder.Property(p => p.TransactionCode)
                   .HasColumnName("transaction_code")
                   .HasMaxLength(100)
                   .IsUnicode(false)
                   .IsRequired();

            // 7. Trạng thái thanh toán.
            // PENDING, PAID, FAILED, REFUNDED.
            builder.Property(p => p.Status)
                   .HasColumnName("status")
                   .HasConversion<string>()
                   .HasMaxLength(30)
                   .IsUnicode(false)
                   .IsRequired();

            builder.Property(p => p.ConfirmedByUserId).HasColumnName("confirmed_by_user_id");
            builder.Property(p => p.ExternalReference).HasColumnName("external_reference").HasMaxLength(100);
            builder.HasOne<User>().WithMany().HasForeignKey(p => p.ConfirmedByUserId)
                   .OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_payments_confirmed_by_user");
            builder.HasIndex(p => p.ExternalReference).IsUnique()
                   .HasDatabaseName("UX_payments_external_reference")
                   .HasFilter("[external_reference] IS NOT NULL");

            // 8. Thời điểm thanh toán thành công.
            // Cho phép NULL khi chưa thanh toán.
            builder.Property(p => p.PaidAt)
                   .HasColumnName("paid_at")
                   .HasColumnType("datetime2")
                   .IsRequired(false);

            // 9. Thời điểm tạo giao dịch.
            builder.Property(p => p.CreatedAt)
                   .HasColumnName("created_at")
                   .HasColumnType("datetime2")
                   .HasDefaultValueSql("SYSDATETIME()")
                   .IsRequired();

            // 10. Mỗi mã giao dịch phải duy nhất.
            builder.HasIndex(p => p.TransactionCode)
                   .IsUnique()
                   .HasDatabaseName("UQ_payments_transaction_code");

            // 11. Quan hệ N - 1:
            // Nhiều giao dịch có thể thuộc một đơn thuê.
            builder.HasOne(p => p.RentalOrder)
                   .WithMany(ro => ro.Payments)
                   .HasForeignKey(p => p.RentalOrderId)
                   .OnDelete(DeleteBehavior.NoAction);
        }
    }
}