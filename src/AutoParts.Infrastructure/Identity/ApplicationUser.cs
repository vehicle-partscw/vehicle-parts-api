using Microsoft.AspNetCore.Identity;

namespace AutoParts.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    // customer-only
    public decimal? CreditLimit { get; set; }
}
