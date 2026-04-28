using AutoParts.Domain.Common;

namespace AutoParts.Domain.Entities;

public class LoyaltyTier : BaseAuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public decimal MinSinglePurchase { get; set; }
    public decimal DiscountPercent { get; set; }
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
    public bool IsActive { get; set; } = true;
}
