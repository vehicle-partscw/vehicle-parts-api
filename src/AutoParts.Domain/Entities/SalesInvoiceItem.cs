using AutoParts.Domain.Common;

namespace AutoParts.Domain.Entities;

public class SalesInvoiceItem : BaseEntity
{
    public Guid SalesInvoiceId { get; set; }
    public Guid PartId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }

    public Part? Part { get; set; }
}
