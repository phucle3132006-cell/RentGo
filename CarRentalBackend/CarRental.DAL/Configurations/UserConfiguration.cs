
using CarRental.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarRental.DAL.Configurations
{
    // Class cấu hình ánh xạ Entity User với bảng users trong SQL Server.
    //
    // IEntityTypeConfiguration<User> là interface của EF Core.
    // Interface này yêu cầu triển khai phương thức Configure().
    //
    // Mục đích:
    // - Xác định tên bảng và schema.
    // - Cấu hình khóa chính.
    // - Ánh xạ thuộc tính C# với cột SQL.
    // - Cấu hình kiểu dữ liệu, độ dài, NULL.
    // - Chuyển đổi Enum thành chuỗi.
    // - Cấu hình giá trị mặc định và ràng buộc UNIQUE.
    // - Cấu hình quan hệ với đơn thuê.
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        // EF Core gọi phương thức này khi xây dựng model.
        //
        // builder là đối tượng dùng để cấu hình Entity User.
        public void Configure(EntityTypeBuilder<User> builder)
        {
            // ==========================================
            // 1. TÊN BẢNG
            // ==========================================

            // Entity User tương ứng với bảng dbo.users.
            builder.ToTable("users", "dbo");


            // ==========================================
            // 2. KHÓA CHÍNH
            // ==========================================

            // Id là khóa chính của Entity User.
            // PK_users là tên PRIMARY KEY trong SQL.
            builder.HasKey(u => u.Id)
                   .HasName("PK_users");

            // Ánh xạ Id với cột id.
            // BIGINT IDENTITY(1,1) tương ứng với long
            // và giá trị được SQL Server tự sinh.
            builder.Property(u => u.Id)
                   .HasColumnName("id")
                   .HasColumnType("bigint")
                   .ValueGeneratedOnAdd();


            // ==========================================
            // 3. HỌ VÀ TÊN
            // ==========================================

            // FullName -> full_name
            // NVARCHAR(100) NOT NULL
            builder.Property(u => u.FullName)
                   .HasColumnName("full_name")
                   .HasMaxLength(100)
                   .IsUnicode(true)
                   .IsRequired();


            // ==========================================
            // 4. EMAIL
            // ==========================================

            // Email -> email
            // VARCHAR(100) NOT NULL
            builder.Property(u => u.Email)
                   .HasColumnName("email")
                   .HasMaxLength(100)
                   .IsUnicode(false)
                   .IsRequired();


            // ==========================================
            // 5. SỐ ĐIỆN THOẠI
            // ==========================================

            // Phone -> phone
            // VARCHAR(20) NOT NULL
            builder.Property(u => u.Phone)
                   .HasColumnName("phone")
                   .HasMaxLength(20)
                   .IsUnicode(false)
                   .IsRequired();


            // ==========================================
            // 6. MẬT KHẨU ĐÃ BĂM
            // ==========================================

            // PasswordHash -> password_hash
            // VARCHAR(255) NOT NULL
            //
            // Chỉ lưu mật khẩu đã được băm an toàn.
            // Không lưu mật khẩu gốc.
            builder.Property(u => u.PasswordHash)
                   .HasColumnName("password_hash")
                   .HasMaxLength(255)
                   .IsUnicode(false)
                   .IsRequired();


            // ==========================================
            // 7. VAI TRÒ NGƯỜI DÙNG
            // ==========================================

            // Role -> role
            // VARCHAR(30) NOT NULL
            //
            // SQL chỉ chấp nhận hai vai trò:
            // CUSTOMER: Khách hàng
            // ADMIN: Quản trị viên
            //
            // HasConversion<string>() chuyển Enum
            // thành tên chuỗi khi lưu vào SQL.
            builder.Property(u => u.Role)
                   .HasColumnName("role")
                   .HasConversion<string>()
                   .HasMaxLength(30)
                   .IsUnicode(false)
                   .IsRequired();


            // ==========================================
            // 8. TRẠNG THÁI TÀI KHOẢN
            // ==========================================

            // Status -> status
            // VARCHAR(30) NOT NULL
            //
            // SQL chỉ chấp nhận ACTIVE hoặc LOCKED.
            builder.Property(u => u.Status)
                   .HasColumnName("status")
                   .HasConversion<string>()
                   .HasMaxLength(30)
                   .IsUnicode(false)
                   .IsRequired();


            // ==========================================
            // 9. THỜI GIAN TẠO
            // ==========================================

            // CreatedAt -> created_at
            // DATETIME2 NOT NULL
            //
            // SYSDATETIME() là giá trị mặc định
            // được SQL Server tạo khi INSERT
            // nếu ứng dụng không cung cấp giá trị.
            builder.Property(u => u.CreatedAt)
                   .HasColumnName("created_at")
                   .HasColumnType("datetime2")
                   .HasDefaultValueSql("SYSDATETIME()")
                   .IsRequired();


            // ==========================================
            // 10. THỜI GIAN CẬP NHẬT
            // ==========================================

            // UpdatedAt -> updated_at
            // DATETIME2 NOT NULL
            //
            // DEFAULT chỉ áp dụng khi INSERT.
            // Khi UPDATE, ứng dụng phải chủ động
            // cập nhật UpdatedAt.
            builder.Property(u => u.UpdatedAt)
                   .HasColumnName("updated_at")
                   .HasColumnType("datetime2")
                   .HasDefaultValueSql("SYSDATETIME()")
                   .IsRequired();


            // ==========================================
            // 11. RÀNG BUỘC UNIQUE
            // ==========================================

            // SQL có UNIQUE CONSTRAINT UQ_users_email.
            // HasAlternateKey biểu diễn UNIQUE constraint
            // thay vì tạo một unique index mới.
            builder.Property(u => u.SessionVersion).HasColumnName("session_version");

            builder.HasAlternateKey(u => u.Email)
                   .HasName("UQ_users_email");

            // Mỗi số điện thoại chỉ thuộc một tài khoản.
            builder.HasAlternateKey(u => u.Phone)
                   .HasName("UQ_users_phone");


            // ==========================================
            // 12. QUAN HỆ USER - RENTAL ORDER
            // ==========================================

            // Một User có thể có nhiều RentalOrder.
            // Mỗi RentalOrder thuộc về một Customer.
            //
            // Khóa ngoại nằm ở RentalOrder.CustomerId.
            //
            // NoAction phù hợp với FK SQL gốc,
            // không tự động xóa đơn khi xóa User.
            builder.HasMany(u => u.RentalOrders)
                   .WithOne(o => o.Customer)
                   .HasForeignKey(o => o.CustomerId)
                   .OnDelete(DeleteBehavior.NoAction)
                   .HasConstraintName("FK_rental_orders_users");
        }
    }
}
