using AutoParts.Domain.Common;

namespace AutoParts.Domain.Entities;

public class PurchaseInvoiceItem : BaseEntity
{
    public Guid PurchaseInvoiceId { get; set; }
    public Guid PartId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal LineTotal { get; set; }

    public Part? Part { get; set; }
}
