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

    public PartCategory? Category { get; set; }
    public Vendor? Vendor { get; set; }
}
