namespace AutoParts.Application.Common.Models;

public class CustomerDto
{
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public bool IsActive { get; set; }
    public decimal? CreditLimit { get; set; }
    public DateTime CreatedAt { get; set; }
}
