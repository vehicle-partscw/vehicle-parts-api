using AutoParts.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoParts.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> b)
    {
        b.ToTable("Notifications");
        b.HasKey(n => n.Id);
        b.Property(n => n.RecipientUserId).IsRequired();
        b.Property(n => n.Type).HasConversion<int>();
        b.Property(n => n.Title).IsRequired().HasMaxLength(140);
        b.Property(n => n.Body).IsRequired();
        b.Property(n => n.RelatedEntityName).HasMaxLength(60);
        b.Property(n => n.RelatedEntityId).HasMaxLength(64);
        b.Property(n => n.IsDeleted).HasDefaultValue(false);
        b.HasIndex(n => new { n.RecipientUserId, n.ReadAt });
    }
}
