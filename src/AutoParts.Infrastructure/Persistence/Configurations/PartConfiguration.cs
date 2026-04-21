using AutoParts.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoParts.Infrastructure.Persistence.Configurations;

public class PartConfiguration : IEntityTypeConfiguration<Part>
{
    public void Configure(EntityTypeBuilder<Part> b)
    {
        b.ToTable("Parts");
        b.HasKey(p => p.Id);

        b.Property(p => p.Sku).IsRequired().HasMaxLength(40);
        b.Property(p => p.Name).IsRequired().HasMaxLength(120);
        b.Property(p => p.Description);
        b.Property(p => p.UnitPrice).HasColumnType("numeric(10,2)");
        b.Property(p => p.StockQty).HasDefaultValue(0);
        b.Property(p => p.ReorderLevel).HasDefaultValue((short)10);
        b.Property(p => p.ImageUrl).HasMaxLength(512);
        b.Property(p => p.IsDeleted).HasDefaultValue(false);

        b.HasIndex(p => p.Sku).IsUnique().HasDatabaseName("IX_Parts_Sku_Unique");
        b.HasIndex(p => p.Name).HasDatabaseName("IX_Parts_Name");

        b.HasOne(p => p.Category).WithMany().HasForeignKey(p => p.CategoryId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(p => p.Vendor).WithMany().HasForeignKey(p => p.VendorId).OnDelete(DeleteBehavior.Restrict);
    }
}
