using AutoParts.Domain.Common;

namespace AutoParts.Domain.Entities;

public class Part : BaseAuditableEntity
{
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid CategoryId { get; set; }
    public Guid VendorId { get; set; }
    public decimal UnitPrice { get; set; }
    public int StockQty { get; set; }
    public short ReorderLevel { get; set; } = 10;
    public string? ImageUrl { get; set; }

    // comma-separated list of vehicle makes this part fits (eg. "Toyota,Honda").
    // null/empty means it's a universal part that fits any vehicle.
    public string? CompatibleMakes { get; set; }

    public PartCategory? Category { get; set; }
    public Vendor? Vendor { get; set; }
}
