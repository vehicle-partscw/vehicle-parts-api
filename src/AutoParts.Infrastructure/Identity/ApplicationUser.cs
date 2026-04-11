using Microsoft.AspNetCore.Identity;

namespace AutoParts.Infrastructure.Identity;

/// <summary>
/// ASP.NET Core Identity user that extends IdentityUser.
/// This is the authentication user with password hashing and other Identity features.
/// StaffProfile in the Domain layer is the lightweight profile representation.
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}
