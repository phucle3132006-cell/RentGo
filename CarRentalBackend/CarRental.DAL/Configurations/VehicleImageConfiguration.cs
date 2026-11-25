using CarRental.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarRental.DAL.Configurations
{
    // Cấu hình ánh xạ Entity VehicleImage
    // với bảng dbo.vehicle_images trong SQL Server.
    public class VehicleImageConfiguration
        : IEntityTypeConfiguration<VehicleImage>
    {
        public void Configure(
            EntityTypeBuilder<VehicleImage> builder)
        {
            // 1. Xác định tên bảng.
            builder.ToTable("vehicle_images", "dbo");

            // 2. Khóa chính.
            builder.HasKey(vi => vi.Id);

            builder.Property(vi => vi.Id)
                   .HasColumnName("id")
                   .ValueGeneratedOnAdd();

            // 3. Khóa ngoại đến bảng vehicles.
            builder.Property(vi => vi.VehicleId)
                   .HasColumnName("vehicle_id")
                   .IsRequired();

            // 4. Đường dẫn hình ảnh.
            // SQL sử dụng VARCHAR(255).
            builder.Property(vi => vi.ImageUrl)
                   .HasColumnName("image_url")
                   .HasMaxLength(255)
                   .IsUnicode(false)
                   .IsRequired();

            // 5. Đánh dấu ảnh đại diện.
            // false = ảnh thường.
            // true  = ảnh đại diện.
            builder.Property(vi => vi.IsPrimary)
                   .HasColumnName("is_primary")
                   .HasColumnType("bit")
                   .IsRequired();

            // 6. Thời gian tạo hình ảnh.
            builder.Property(vi => vi.CreatedAt)
                   .HasColumnName("created_at")
                   .HasColumnType("datetime2")
                   .HasDefaultValueSql("SYSDATETIME()")
                   .IsRequired();

            // 7. Không cho phép một xe có
            // hai bản ghi cùng đường dẫn ảnh.
            builder.HasIndex(vi => new
            {
                vi.VehicleId,
                vi.ImageUrl
            })
            .IsUnique()
            .HasDatabaseName(
                "UQ_vehicle_images_vehicle_image"
            );

            // 8. Filtered Unique Index:
            // Mỗi xe chỉ có tối đa một ảnh đại diện.
            builder.HasIndex(vi => vi.VehicleId)
                   .IsUnique()
                   .HasFilter("[is_primary] = 1")
                   .HasDatabaseName(
                       "UX_vehicle_images_one_primary"
                   );

            // 9. Quan hệ N - 1:
            // Nhiều ảnh thuộc về một xe.
            builder.HasOne(vi => vi.Vehicle)
                   .WithMany(v => v.VehicleImages)
                   .HasForeignKey(vi => vi.VehicleId)
                   .OnDelete(DeleteBehavior.NoAction);
        }
    }
}