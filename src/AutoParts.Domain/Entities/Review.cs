using AutoParts.Domain.Common;

namespace AutoParts.Domain.Entities;

public class Review : BaseAuditableEntity
{
    public string CustomerUserId { get; set; } = string.Empty;
    public short Rating { get; set; }
    public string? Comment { get; set; }
}
