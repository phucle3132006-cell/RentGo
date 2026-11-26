using CarRental.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarRental.DAL.Configurations;

public sealed class UserBlacklistConfiguration : IEntityTypeConfiguration<UserBlacklistEntry>
{
    public void Configure(EntityTypeBuilder<UserBlacklistEntry> builder)
    {
        builder.ToTable("user_blacklist", "dbo");
        builder.HasKey(entry => entry.UserId);
        builder.Property(entry => entry.UserId).HasColumnName("user_id").ValueGeneratedNever();
        builder.Property(entry => entry.Email).HasColumnName("email").HasMaxLength(100).IsUnicode(false).IsRequired();
        builder.Property(entry => entry.Phone).HasColumnName("phone").HasMaxLength(20).IsUnicode(false).IsRequired();
        builder.Property(entry => entry.PhoneKey).HasColumnName("phone_key").HasMaxLength(20).IsUnicode(false)
            .HasComputedColumnSql("[dbo].[rentgo_blacklist_phone]([phone])", stored: true);
        builder.Property(entry => entry.Reason).HasColumnName("reason").HasMaxLength(500).IsRequired();
        builder.Property(entry => entry.BlockedAt).HasColumnName("blocked_at");
        builder.Property(entry => entry.BlockedByUserId).HasColumnName("blocked_by_user_id");
        builder.HasOne(entry => entry.User).WithMany().HasForeignKey(entry => entry.UserId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(entry => entry.BlockedBy).WithMany().HasForeignKey(entry => entry.BlockedByUserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
