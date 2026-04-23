using AutoParts.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoParts.Infrastructure.Persistence.Configurations;

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> b)
    {
        b.ToTable("Appointments");
        b.HasKey(a => a.Id);
        b.Property(a => a.CustomerUserId).IsRequired();
        b.Property(a => a.Notes);
        b.Property(a => a.IsDeleted).HasDefaultValue(false);
        b.Property(a => a.Status).HasConversion<int>();

        b.HasOne(a => a.Vehicle).WithMany().HasForeignKey(a => a.VehicleId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(a => a.ServiceType).WithMany().HasForeignKey(a => a.ServiceTypeId).OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(a => a.CustomerUserId).HasDatabaseName("IX_Appointments_CustomerUserId");
        b.HasIndex(a => a.ScheduledAt).HasDatabaseName("IX_Appointments_ScheduledAt");
    }
}
