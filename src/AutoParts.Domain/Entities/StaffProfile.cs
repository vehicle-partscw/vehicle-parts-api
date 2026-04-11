using AutoParts.Domain.Common;

namespace AutoParts.Domain.Entities;

/// <summary>
/// Lightweight domain representation of a user.
/// The full Identity user (with PasswordHash etc.) lives in Infrastructure.
/// This entity is used for domain relationships (e.g., CreatedBy on invoices).
/// </summary>
public class StaffProfile : BaseAuditableEntity
{
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
