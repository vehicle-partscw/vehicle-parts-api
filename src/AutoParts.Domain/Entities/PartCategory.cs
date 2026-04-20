using AutoParts.Domain.Common;

namespace AutoParts.Domain.Entities;

public class PartCategory : BaseAuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
