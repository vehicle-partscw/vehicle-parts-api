using AutoParts.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoParts.Infrastructure.Persistence.Configurations;

public class ServiceTypeConfiguration : IEntityTypeConfiguration<ServiceType>
{
    public void Configure(EntityTypeBuilder<ServiceType> b)
    {
        b.ToTable("ServiceTypes");
        b.HasKey(s => s.Id);
        b.Property(s => s.Code).IsRequired().HasMaxLength(20);
        b.Property(s => s.Name).IsRequired().HasMaxLength(100);
        b.Property(s => s.Description);
        b.Property(s => s.BasePrice).HasColumnType("numeric(10,2)");
        b.Property(s => s.IsActive).HasDefaultValue(true);
        b.Property(s => s.IsDeleted).HasDefaultValue(false);
        b.HasIndex(s => s.Code).IsUnique().HasDatabaseName("IX_ServiceTypes_Code_Unique");
    }
}
