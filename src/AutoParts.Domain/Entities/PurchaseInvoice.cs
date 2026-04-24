using AutoParts.Domain.Common;

namespace AutoParts.Domain.Entities;

public class PurchaseInvoice : BaseAuditableEntity
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public Guid VendorId { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;
    public DateOnly IssueDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }

    public Vendor? Vendor { get; set; }
    public List<PurchaseInvoiceItem> Items { get; set; } = new();
}
