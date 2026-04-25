using AutoParts.Domain.Common;
using AutoParts.Domain.Enums;

namespace AutoParts.Domain.Entities;

public class Payment : BaseAuditableEntity
{
    public Guid SalesInvoiceId { get; set; }
    public string ReceivedByUserId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public string? ReferenceNo { get; set; }
    public DateTime PaidAt { get; set; }
    public string? Notes { get; set; }
}
