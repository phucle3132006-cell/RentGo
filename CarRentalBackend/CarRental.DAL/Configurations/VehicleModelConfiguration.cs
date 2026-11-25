using CarRental.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarRental.DAL.Configurations
{
    // Cấu hình ánh xạ Entity VehicleModel
    // với bảng vehicle_models trong SQL Server.
    public class VehicleModelConfiguration
        : IEntityTypeConfiguration<VehicleModel>
    {
        public void Configure(
            EntityTypeBuilder<VehicleModel> builder)
        {
            // 1. Tên bảng và schema.
            builder.ToTable("vehicle_models", "dbo");

            // 2. Khóa chính.
            builder.HasKey(vm => vm.Id);

            builder.Property(vm => vm.Id)
                   .HasColumnName("id")
                   .ValueGeneratedOnAdd();

            // 3. Loại phương tiện.
            // Enum được lưu dưới dạng chuỗi:
            // XE_SO, XE_GA, XE_DIEN, OTO.
            builder.Property(vm => vm.Category)
                   .HasColumnName("category")
                   .HasConversion<string>()
                   .HasMaxLength(30)
                   .IsUnicode(false)
                   .IsRequired();

            // 4. Hãng xe.
            builder.Property(vm => vm.Brand)
                   .HasColumnName("brand")
                   .HasMaxLength(50)
                   .IsUnicode(false)
                   .IsRequired();

            // 5. Tên mẫu xe.
            builder.Property(vm => vm.Model)
                   .HasColumnName("model")
                   .HasMaxLength(100)
                   .IsUnicode(false)
                   .IsRequired();

            // 6. Năm sản xuất.
            builder.Property(vm => vm.Year)
                   .HasColumnName("year")
                   .IsRequired();

            // 7. Số chỗ ngồi.
            builder.Property(vm => vm.Seats)
                   .HasColumnName("seats")
                   .IsRequired();

            // 8. Dung tích động cơ.
            // Có thể NULL trong database.
            builder.Property(vm => vm.EngineCapacity)
                   .HasColumnName("engine_capacity")
                   .IsRequired(false);

            // 9. Mô tả xe.
            // NVARCHAR(MAX), cho phép NULL.
            builder.Property(vm => vm.Description)
                   .HasColumnName("description")
                   .HasColumnType("nvarchar(max)")
                   .IsRequired(false);

            // 10. Giá thuê mỗi ngày.
            builder.Property(vm => vm.PricePerDay)
                   .HasColumnName("price_per_day")
                   .HasColumnType("decimal(10,2)")
                   .IsRequired();

            // 11. Ngày tạo.
            builder.Property(vm => vm.CreatedAt)
                   .HasColumnName("created_at")
                   .HasColumnType("datetime2")
                   .HasDefaultValueSql("SYSDATETIME()")
                   .IsRequired();

            // 12. Ngày cập nhật.
            builder.Property(vm => vm.UpdatedAt)
                   .HasColumnName("updated_at")
                   .HasColumnType("datetime2")
                   .HasDefaultValueSql("SYSDATETIME()")
                   .IsRequired();

            // 13. Ràng buộc UNIQUE:
            // Không được trùng hãng + mẫu xe + năm.
            builder.HasIndex(vm => new
            {
                vm.Brand,
                vm.Model,
                vm.Year
            })
            .IsUnique()
            .HasDatabaseName("UQ_vehicle_models_brand_model_year");

            // 14. Quan hệ 1 - N:
            // Một dòng xe có thể có nhiều xe thực tế.
            builder.HasMany(vm => vm.Vehicles)
                   .WithOne(v => v.VehicleModel)
                   .HasForeignKey(v => v.VehicleModelId)
                   .OnDelete(DeleteBehavior.NoAction);
        }
    }
}