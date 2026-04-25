using AutoParts.Domain.Common;

namespace AutoParts.Domain.Entities;

public class AiPrediction : BaseAuditableEntity
{
    public Guid VehicleId { get; set; }
    public Guid PartId { get; set; }
    public decimal FailureProbability { get; set; }
    public short WindowDays { get; set; }
    public string ModelVersion { get; set; } = string.Empty;
    public DateTime PredictedAt { get; set; }
}
