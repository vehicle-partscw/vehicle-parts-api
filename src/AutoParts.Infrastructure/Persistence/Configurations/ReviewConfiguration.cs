using AutoParts.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoParts.Infrastructure.Persistence.Configurations;

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> b)
    {
        b.ToTable("Reviews", t => t.HasCheckConstraint("CK_Reviews_Rating", "\"Rating\" between 1 and 5"));
        b.HasKey(r => r.Id);
        b.Property(r => r.CustomerUserId).IsRequired();
        b.Property(r => r.Comment);
        b.Property(r => r.IsDeleted).HasDefaultValue(false);
    }
}
