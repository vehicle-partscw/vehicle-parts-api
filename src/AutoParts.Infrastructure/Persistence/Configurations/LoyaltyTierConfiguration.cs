using AutoParts.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoParts.Infrastructure.Persistence.Configurations;

public class LoyaltyTierConfiguration : IEntityTypeConfiguration<LoyaltyTier>
{
    public void Configure(EntityTypeBuilder<LoyaltyTier> b)
    {
        b.ToTable("LoyaltyTiers");
        b.HasKey(t => t.Id);
        b.Property(t => t.Name).IsRequired().HasMaxLength(50);
        b.Property(t => t.MinSinglePurchase).HasColumnType("numeric(12,2)");
        b.Property(t => t.DiscountPercent).HasColumnType("numeric(5,2)");
        b.Property(t => t.IsActive).HasDefaultValue(true);
        b.Property(t => t.IsDeleted).HasDefaultValue(false);
        b.HasIndex(t => t.Name).IsUnique().HasDatabaseName("IX_LoyaltyTiers_Name_Unique");
    }
}
