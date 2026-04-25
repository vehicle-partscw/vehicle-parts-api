using AutoParts.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoParts.Infrastructure.Persistence.Configurations;

public class AiPredictionConfiguration : IEntityTypeConfiguration<AiPrediction>
{
    public void Configure(EntityTypeBuilder<AiPrediction> b)
    {
        b.ToTable("AiPredictions");
        b.HasKey(p => p.Id);
        b.Property(p => p.FailureProbability).HasColumnType("numeric(4,3)");
        b.Property(p => p.ModelVersion).HasMaxLength(20);
        b.Property(p => p.IsDeleted).HasDefaultValue(false);
        b.HasIndex(p => p.VehicleId);
        b.HasIndex(p => p.PartId);
    }
}
