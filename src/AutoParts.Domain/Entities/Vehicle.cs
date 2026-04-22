using AutoParts.Domain.Common;

namespace AutoParts.Domain.Entities;

public class Vehicle : BaseAuditableEntity
{
    public string CustomerUserId { get; set; } = string.Empty;
    public string VehicleNumber { get; set; } = string.Empty;
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public short Year { get; set; }
    public string? Vin { get; set; }
    public int Mileage { get; set; }
}
