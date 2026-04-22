using AutoParts.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoParts.Infrastructure.Persistence.Configurations;

public class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> b)
    {
        b.ToTable("Vehicles");
        b.HasKey(v => v.Id);

        b.Property(v => v.CustomerUserId).IsRequired();
        b.Property(v => v.VehicleNumber).IsRequired().HasMaxLength(20);
        b.Property(v => v.Make).IsRequired().HasMaxLength(40);
        b.Property(v => v.Model).IsRequired().HasMaxLength(60);
        b.Property(v => v.Vin).HasMaxLength(17);
        b.Property(v => v.Mileage).HasDefaultValue(0);
        b.Property(v => v.IsDeleted).HasDefaultValue(false);

        b.HasIndex(v => v.VehicleNumber).IsUnique().HasDatabaseName("IX_Vehicles_VehicleNumber_Unique");
        b.HasIndex(v => v.CustomerUserId).HasDatabaseName("IX_Vehicles_CustomerUserId");
    }
}
