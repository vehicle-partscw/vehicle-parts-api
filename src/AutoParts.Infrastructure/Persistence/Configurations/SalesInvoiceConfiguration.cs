using AutoParts.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoParts.Infrastructure.Persistence.Configurations;

public class SalesInvoiceConfiguration : IEntityTypeConfiguration<SalesInvoice>
{
    public void Configure(EntityTypeBuilder<SalesInvoice> b)
    {
        b.ToTable("SalesInvoices");
        b.HasKey(s => s.Id);
        b.Property(s => s.InvoiceNumber).IsRequired().HasMaxLength(32);
        b.Property(s => s.CustomerUserId).IsRequired();
        b.Property(s => s.CreatedByUserId).IsRequired();
        b.Property(s => s.Subtotal).HasColumnType("numeric(12,2)");
        b.Property(s => s.DiscountAmount).HasColumnType("numeric(8,2)");
        b.Property(s => s.TotalAmount).HasColumnType("numeric(12,2)");
        b.Property(s => s.AmountPaid).HasColumnType("numeric(12,2)").HasDefaultValue(0m);
        b.Property(s => s.AmountDue).HasColumnType("numeric(12,2)");
        b.Property(s => s.PaymentStatus).HasConversion<int>();
        b.Property(s => s.IsDeleted).HasDefaultValue(false);

        b.HasIndex(s => s.InvoiceNumber).IsUnique().HasDatabaseName("IX_SalesInvoices_InvoiceNumber_Unique");
        b.HasIndex(s => new { s.PaymentStatus, s.IssueDate });
        b.HasIndex(s => s.CustomerUserId);
        b.HasIndex(s => s.RelatedAppointmentId);

        b.HasOne(s => s.LoyaltyTier).WithMany().HasForeignKey(s => s.LoyaltyTierId).OnDelete(DeleteBehavior.SetNull);
        b.HasMany(s => s.Items).WithOne().HasForeignKey(i => i.SalesInvoiceId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(s => s.Payments).WithOne().HasForeignKey(p => p.SalesInvoiceId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class SalesInvoiceItemConfiguration : IEntityTypeConfiguration<SalesInvoiceItem>
{
    public void Configure(EntityTypeBuilder<SalesInvoiceItem> b)
    {
        b.ToTable("SalesInvoiceItems");
        b.HasKey(i => i.Id);
        b.Property(i => i.UnitPrice).HasColumnType("numeric(10,2)");
        b.Property(i => i.LineTotal).HasColumnType("numeric(12,2)");
        b.HasOne(i => i.Part).WithMany().HasForeignKey(i => i.PartId).OnDelete(DeleteBehavior.Restrict);
    }
}
