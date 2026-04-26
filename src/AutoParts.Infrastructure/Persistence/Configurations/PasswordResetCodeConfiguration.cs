using AutoParts.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoParts.Infrastructure.Persistence.Configurations;

public class PasswordResetCodeConfiguration : IEntityTypeConfiguration<PasswordResetCode>
{
    public void Configure(EntityTypeBuilder<PasswordResetCode> b)
    {
        b.ToTable("PasswordResetCodes");
        b.HasKey(x => x.Id);
        b.Property(x => x.UserId).IsRequired().HasMaxLength(450);
        b.Property(x => x.CodeHash).IsRequired().HasMaxLength(128);
        b.Property(x => x.ExpiresAt).IsRequired();
        b.Property(x => x.IsDeleted).HasDefaultValue(false);
        b.HasIndex(x => x.UserId);
    }
}
