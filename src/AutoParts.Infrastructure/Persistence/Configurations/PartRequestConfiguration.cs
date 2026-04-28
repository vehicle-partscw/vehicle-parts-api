using AutoParts.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoParts.Infrastructure.Persistence.Configurations;

public class PartRequestConfiguration : IEntityTypeConfiguration<PartRequest>
{
    public void Configure(EntityTypeBuilder<PartRequest> b)
    {
        b.ToTable("PartRequests");
        b.HasKey(p => p.Id);
        b.Property(p => p.CustomerUserId).IsRequired();
        b.Property(p => p.PartName).IsRequired().HasMaxLength(120);
        b.Property(p => p.Description);
        b.Property(p => p.Status).HasConversion<int>();
        b.Property(p => p.IsDeleted).HasDefaultValue(false);
        b.HasIndex(p => p.CustomerUserId);

        b.HasOne(p => p.ResolvedPart)
            .WithMany()
            .HasForeignKey(p => p.ResolvedPartId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
