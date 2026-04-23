using AutoParts.Domain.Common;
using AutoParts.Domain.Enums;

namespace AutoParts.Domain.Entities;

public class Appointment : BaseAuditableEntity
{
    public string CustomerUserId { get; set; } = string.Empty;
    public Guid VehicleId { get; set; }
    public Guid ServiceTypeId { get; set; }
    public string? AssignedStaffUserId { get; set; }
    public DateTime ScheduledAt { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;
    public string? Notes { get; set; }

    public Vehicle? Vehicle { get; set; }
    public ServiceType? ServiceType { get; set; }
}
