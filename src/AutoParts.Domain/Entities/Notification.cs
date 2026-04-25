using AutoParts.Domain.Common;

namespace AutoParts.Domain.Entities;

public class Notification : BaseAuditableEntity
{
    public string RecipientUserId { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? RelatedEntityName { get; set; }
    public string? RelatedEntityId { get; set; }
    public DateTime? ReadAt { get; set; }
}

public enum NotificationType
{
    LowStock = 0,
    OverdueCredit = 1,
    AppointmentReminder = 2,
    PartRequest = 3,
    AiAlert = 4
}
