using AutoParts.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoParts.Infrastructure.Persistence.Configurations;

public class PurchaseInvoiceConfiguration : IEntityTypeConfiguration<PurchaseInvoice>
{
    public void Configure(EntityTypeBuilder<PurchaseInvoice> b)
    {
        b.ToTable("PurchaseInvoices");
        b.HasKey(p => p.Id);
        b.Property(p => p.InvoiceNumber).IsRequired().HasMaxLength(32);
        b.Property(p => p.CreatedByUserId).IsRequired();
        b.Property(p => p.TotalAmount).HasColumnType("numeric(12,2)");
        b.Property(p => p.Notes);
        b.Property(p => p.IsDeleted).HasDefaultValue(false);

        b.HasIndex(p => p.InvoiceNumber).IsUnique().HasDatabaseName("IX_PurchaseInvoices_InvoiceNumber_Unique");
        b.HasOne(p => p.Vendor).WithMany().HasForeignKey(p => p.VendorId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(p => p.Items).WithOne().HasForeignKey(i => i.PurchaseInvoiceId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class PurchaseInvoiceItemConfiguration : IEntityTypeConfiguration<PurchaseInvoiceItem>
{
    public void Configure(EntityTypeBuilder<PurchaseInvoiceItem> b)
    {
        b.ToTable("PurchaseInvoiceItems");
        b.HasKey(i => i.Id);
        b.Property(i => i.UnitCost).HasColumnType("numeric(10,2)");
        b.Property(i => i.LineTotal).HasColumnType("numeric(12,2)");
        b.HasOne(i => i.Part).WithMany().HasForeignKey(i => i.PartId).OnDelete(DeleteBehavior.Restrict);
    }
}
