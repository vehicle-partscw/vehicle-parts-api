using AutoParts.Domain.Common;

namespace AutoParts.Domain.Entities;

public class PasswordResetCode : BaseAuditableEntity
{
    public string UserId { get; set; } = string.Empty;
    public string CodeHash { get; set; } = string.Empty;   // SHA-256 hash of the 6-digit code, never plaintext
    public DateTime ExpiresAt { get; set; }
    public DateTime? ConsumedAt { get; set; }
    public short FailedAttempts { get; set; }              // lockout after too many wrong codes
}
