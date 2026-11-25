using CarRental.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarRental.DAL.Configurations
{
    // Cấu hình ánh xạ Entity Vehicle
    // với bảng dbo.vehicles trong SQL Server.
    public class VehicleConfiguration
        : IEntityTypeConfiguration<Vehicle>
    {
        public void Configure(EntityTypeBuilder<Vehicle> builder)
        {
            // 1. Xác định tên bảng và schema.
            builder.ToTable("vehicles", "dbo");

            // 2. Khóa chính.
            builder.HasKey(v => v.Id);

            builder.Property(v => v.Id)
                   .HasColumnName("id")
                   .ValueGeneratedOnAdd();

            // 3. Khóa ngoại đến bảng vehicle_models.
            builder.Property(v => v.VehicleModelId)
                   .HasColumnName("vehicle_model_id")
                   .IsRequired();

            // 4. Biển số xe.
            builder.Property(v => v.LicensePlate)
                   .HasColumnName("license_plate")
                   .HasMaxLength(20)
                   .IsUnicode(false)
                   .IsRequired();

            // 5. Màu sắc.
            builder.Property(v => v.Color)
                   .HasColumnName("color")
                   .HasMaxLength(30)
                   .IsUnicode()
                   .IsRequired();

            // 6. Tình trạng thực tế của xe.
            builder.Property(v => v.Condition)
                   .HasColumnName("condition")
                   .HasMaxLength(100)
                   .IsUnicode()
                   .IsRequired();

            // 7. Trạng thái xe.
            // Enum được lưu thành chuỗi:
            // AVAILABLE, RENTED, MAINTENANCE.
            builder.Property(v => v.Status)
                   .HasColumnName("status")
                   .HasConversion<string>()
                   .HasMaxLength(30)
                   .IsUnicode(false)
                   .IsRequired();

            // 8. Thời gian tạo.
            builder.Property(v => v.CreatedAt)
                   .HasColumnName("created_at")
                   .HasColumnType("datetime2")
                   .HasDefaultValueSql("SYSDATETIME()")
                   .IsRequired();

            // 9. Thời gian cập nhật.
            builder.Property(v => v.UpdatedAt)
                   .HasColumnName("updated_at")
                   .HasColumnType("datetime2")
                   .HasDefaultValueSql("SYSDATETIME()")
                   .IsRequired();

            // 10. Biển số xe không được trùng.
            builder.HasIndex(v => v.LicensePlate)
                   .IsUnique()
                   .HasDatabaseName("UQ_vehicles_license_plate");

            // 11. Quan hệ N - 1:
            // Nhiều xe thực tế thuộc một dòng xe.
            builder.HasOne(v => v.VehicleModel)
                   .WithMany(vm => vm.Vehicles)
                   .HasForeignKey(v => v.VehicleModelId)
                   .OnDelete(DeleteBehavior.NoAction);

            // 12. Quan hệ 1 - N:
            // Một xe có thể có nhiều hình ảnh.
            builder.HasMany(v => v.VehicleImages)
                   .WithOne(vi => vi.Vehicle)
                   .HasForeignKey(vi => vi.VehicleId)
                   .OnDelete(DeleteBehavior.NoAction);

            // 13. Quan hệ 1 - N:
            // Một xe có thể xuất hiện trong nhiều
            // chi tiết đơn thuê khác nhau.
            builder.HasMany(v => v.RentalOrderDetails)
                   .WithOne(rod => rod.Vehicle)
                   .HasForeignKey(rod => rod.VehicleId)
                   .OnDelete(DeleteBehavior.NoAction);
        }
    }
}