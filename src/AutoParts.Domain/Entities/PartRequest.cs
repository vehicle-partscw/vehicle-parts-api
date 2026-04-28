using AutoParts.Domain.Common;

namespace AutoParts.Domain.Entities;

public class PartRequest : BaseAuditableEntity
{
    public string CustomerUserId { get; set; } = string.Empty;
    public string PartName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public PartRequestStatus Status { get; set; } = PartRequestStatus.Pending;
    public DateTime RequestedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public Guid? ResolvedPartId { get; set; }   // populated when staff sources the request by adding a catalog entry
    public Part? ResolvedPart { get; set; }
}

public enum PartRequestStatus
{
    Pending = 0,
    Sourced = 1,
    Rejected = 2
}
