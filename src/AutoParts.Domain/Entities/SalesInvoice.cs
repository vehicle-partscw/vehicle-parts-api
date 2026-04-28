using AutoParts.Domain.Common;
using AutoParts.Domain.Enums;

namespace AutoParts.Domain.Entities;

public class SalesInvoice : BaseAuditableEntity
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public string CustomerUserId { get; set; } = string.Empty;
    public string CreatedByUserId { get; set; } = string.Empty;
    public Guid? LoyaltyTierId { get; set; }
    public Guid? RelatedAppointmentId { get; set; } // set when the invoice was created from a Done appointment

    public DateOnly IssueDate { get; set; }
    public DateOnly? DueDate { get; set; }

    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal AmountDue { get; set; }
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;

    public LoyaltyTier? LoyaltyTier { get; set; }
    public List<SalesInvoiceItem> Items { get; set; } = new();
    public List<Payment> Payments { get; set; } = new();
}
