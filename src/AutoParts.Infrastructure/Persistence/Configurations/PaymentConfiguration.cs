using AutoParts.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoParts.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> b)
    {
        b.ToTable("Payments");
        b.HasKey(p => p.Id);
        b.Property(p => p.ReceivedByUserId).IsRequired();
        b.Property(p => p.Amount).HasColumnType("numeric(12,2)");
        b.Property(p => p.Method).HasConversion<int>();
        b.Property(p => p.ReferenceNo).HasMaxLength(60);
        b.Property(p => p.Notes);
        b.Property(p => p.IsDeleted).HasDefaultValue(false);
        b.HasIndex(p => p.SalesInvoiceId);
    }
}
