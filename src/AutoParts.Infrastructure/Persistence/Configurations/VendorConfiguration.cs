using AutoParts.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoParts.Infrastructure.Persistence.Configurations;

public class VendorConfiguration : IEntityTypeConfiguration<Vendor>
{
    public void Configure(EntityTypeBuilder<Vendor> builder)
    {
        builder.ToTable("Vendors");
        builder.HasKey(v => v.Id);

        builder.Property(v => v.Name).IsRequired().HasMaxLength(120);
        builder.Property(v => v.ContactPerson).HasMaxLength(80);
        builder.Property(v => v.Phone).HasMaxLength(20);
        builder.Property(v => v.Email).HasMaxLength(256);
        builder.Property(v => v.Address);
        builder.Property(v => v.IsActive).HasDefaultValue(true);
        builder.Property(v => v.IsDeleted).HasDefaultValue(false);

        builder.HasIndex(v => v.Name).HasDatabaseName("IX_Vendors_Name");
    }
}
