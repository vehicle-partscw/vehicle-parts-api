using AutoParts.Domain.Common;

namespace AutoParts.Domain.Entities;

public class ServiceType : BaseAuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal BasePrice { get; set; }
    public short EstimatedMinutes { get; set; }
    public bool IsActive { get; set; } = true;
}
