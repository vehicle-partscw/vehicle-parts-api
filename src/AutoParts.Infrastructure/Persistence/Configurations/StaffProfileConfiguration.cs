using AutoParts.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoParts.Infrastructure.Persistence.Configurations;

public class StaffProfileConfiguration : IEntityTypeConfiguration<StaffProfile>
{
    public void Configure(EntityTypeBuilder<StaffProfile> builder)
    {
        builder.ToTable("StaffProfiles");

        builder.HasKey(sp => sp.Id);

        builder.Property(sp => sp.UserId)
            .IsRequired();

        builder.Property(sp => sp.FullName)
            .IsRequired();

        builder.Property(sp => sp.Email)
            .IsRequired();

        builder.Property(sp => sp.Phone)
            .IsRequired(false);

        builder.Property(sp => sp.Role)
            .IsRequired();

        builder.Property(sp => sp.IsActive)
            .HasDefaultValue(true);

        builder.Property(sp => sp.CreatedAt)
            .IsRequired();

        builder.Property(sp => sp.CreatedBy)
            .IsRequired(false);

        builder.Property(sp => sp.UpdatedAt)
            .IsRequired(false);

        builder.Property(sp => sp.UpdatedBy)
            .IsRequired(false);

        builder.Property(sp => sp.IsDeleted)
            .HasDefaultValue(false);

        // Indexes
        builder.HasIndex(sp => sp.UserId)
            .IsUnique()
            .HasDatabaseName("IX_StaffProfiles_UserId_Unique");

        builder.HasIndex(sp => sp.Email)
            .HasDatabaseName("IX_StaffProfiles_Email");
    }
}
