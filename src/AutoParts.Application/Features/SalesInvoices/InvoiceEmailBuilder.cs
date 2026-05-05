using System.Globalization;
using System.Text;

namespace AutoParts.Application.Features.SalesInvoices;

// builds the email subject, html body and plain text body for a sales invoice email.
// kept separate from the handler so it can be unit tested in isolation if needed.
public static class InvoiceEmailBuilder
{
    public record EmailContent(string Subject, string HtmlBody, string TextBody);

    public static EmailContent Build(SalesInvoiceDto invoice, string customerFullName, string siteUrl)
    {
        var subject = $"Your AutoParts invoice {invoice.InvoiceNumber}";

        var html = BuildHtml(invoice, customerFullName, siteUrl);
        var text = BuildText(invoice, customerFullName, siteUrl);

        return new EmailContent(subject, html, text);
    }

    private static string BuildHtml(SalesInvoiceDto invoice, string customerFullName, string siteUrl)
    {
        var sb = new StringBuilder();
        var currency = CultureInfo.GetCultureInfo("en-NP");

        string fmt(decimal value) => "Rs. " + value.ToString("N2", currency);

        sb.Append(@"<!DOCTYPE html>
<html><head><meta charset=""utf-8""></head>
<body style=""font-family: Arial, Helvetica, sans-serif; color:#1A0F0C; background:#FAF6F2; padding:24px 0; margin:0;"">
  <div style=""max-width:600px; margin:auto; background:#FFFFFF; border-radius:14px; overflow:hidden; box-shadow:0 4px 16px rgba(0,0,0,0.06);"">
    <div style=""background:#E54D2E; padding:24px 28px; color:#FFFFFF;"">
      <div style=""font-size:13px; letter-spacing:2px; opacity:0.85;"">AUTOPARTS</div>
      <div style=""font-size:22px; font-weight:700; margin-top:4px;"">Invoice ");
        sb.Append(invoice.InvoiceNumber);
        sb.Append(@"</div>
    </div>
    <div style=""padding:24px 28px;"">
      <p style=""margin:0 0 12px 0;"">Hi ");
        sb.Append(System.Net.WebUtility.HtmlEncode(customerFullName));
        sb.Append(@",</p>
      <p style=""margin:0 0 16px 0;"">Thanks for shopping with AutoParts. Your invoice is attached as a PDF and the details are below for your records.</p>

      <div style=""font-size:13px; color:#5C271F; margin-bottom:14px;"">
        <strong>Invoice number:</strong> ");
        sb.Append(invoice.InvoiceNumber);
        sb.Append(@"<br/>
        <strong>Issue date:</strong> ");
        sb.Append(invoice.IssueDate.ToString("dd MMM yyyy", CultureInfo.InvariantCulture));
        if (invoice.DueDate.HasValue)
        {
            sb.Append(@"<br/>
        <strong>Due date:</strong> ");
            sb.Append(invoice.DueDate.Value.ToString("dd MMM yyyy", CultureInfo.InvariantCulture));
        }
        sb.Append(@"
      </div>

      <table style=""width:100%; border-collapse:collapse; margin-bottom:18px;"">
        <thead>
          <tr style=""background:#FFE7E0; color:#5C271F; text-align:left;"">
            <th style=""padding:8px 10px; font-size:13px;"">Item</th>
            <th style=""padding:8px 10px; font-size:13px; text-align:center;"">Qty</th>
            <th style=""padding:8px 10px; font-size:13px; text-align:right;"">Unit price</th>
            <th style=""padding:8px 10px; font-size:13px; text-align:right;"">Line total</th>
          </tr>
        </thead>
        <tbody>");
        foreach (var item in invoice.Items)
        {
            sb.Append(@"
          <tr style=""border-bottom:1px solid #F1E7E0;"">
            <td style=""padding:8px 10px; font-size:13px;"">");
            sb.Append(System.Net.WebUtility.HtmlEncode(item.PartName));
            sb.Append(@" <span style=""color:#999; font-size:12px;"">(");
            sb.Append(System.Net.WebUtility.HtmlEncode(item.Sku));
            sb.Append(@")</span></td>
            <td style=""padding:8px 10px; font-size:13px; text-align:center;"">");
            sb.Append(item.Quantity);
            sb.Append(@"</td>
            <td style=""padding:8px 10px; font-size:13px; text-align:right;"">");
            sb.Append(fmt(item.UnitPrice));
            sb.Append(@"</td>
            <td style=""padding:8px 10px; font-size:13px; text-align:right;"">");
            sb.Append(fmt(item.LineTotal));
            sb.Append(@"</td>
          </tr>");
        }
        sb.Append(@"
        </tbody>
      </table>

      <div style=""border-top:1px solid #F1E7E0; padding-top:12px; font-size:13px;"">
        <div style=""display:flex; justify-content:space-between; padding:4px 0;""><span>Subtotal</span><span>");
        sb.Append(fmt(invoice.Subtotal));
        sb.Append(@"</span></div>");
        if (invoice.DiscountAmount > 0)
        {
            sb.Append(@"
        <div style=""display:flex; justify-content:space-between; padding:4px 0; color:#2E7D32;""><span>Loyalty discount");
            if (!string.IsNullOrEmpty(invoice.LoyaltyTierName))
            {
                sb.Append(" (");
                sb.Append(System.Net.WebUtility.HtmlEncode(invoice.LoyaltyTierName));
                sb.Append(")");
            }
            sb.Append(@"</span><span>- ");
            sb.Append(fmt(invoice.DiscountAmount));
            sb.Append(@"</span></div>");
        }
        sb.Append(@"
        <div style=""display:flex; justify-content:space-between; padding:6px 0; font-weight:700; font-size:15px; color:#5C271F;""><span>Total</span><span>");
        sb.Append(fmt(invoice.TotalAmount));
        sb.Append(@"</span></div>");
        if (invoice.AmountPaid > 0)
        {
            sb.Append(@"
        <div style=""display:flex; justify-content:space-between; padding:4px 0;""><span>Paid</span><span>");
            sb.Append(fmt(invoice.AmountPaid));
            sb.Append(@"</span></div>");
        }
        if (invoice.AmountDue > 0)
        {
            sb.Append(@"
        <div style=""display:flex; justify-content:space-between; padding:6px 0; font-weight:700; color:#E54D2E;""><span>Balance due</span><span>");
            sb.Append(fmt(invoice.AmountDue));
            sb.Append(@"</span></div>");
        }
        sb.Append(@"
      </div>

      <p style=""margin:24px 0 8px 0; font-size:13px; color:#5C271F;"">You can also view this invoice and your full purchase history online:</p>
      <p style=""margin:0 0 24px 0;""><a href=""");
        sb.Append(siteUrl);
        sb.Append(@"/my-history"" style=""display:inline-block; background:#E54D2E; color:#FFFFFF; padding:10px 18px; border-radius:8px; text-decoration:none; font-weight:600; font-size:13px;"">View on AutoParts</a></p>

      <p style=""margin:0; font-size:12px; color:#888;"">If you have any questions about this invoice please reply to this email and our team will get back to you.</p>
    </div>
  </div>
</body></html>");

        return sb.ToString();
    }

    private static string BuildText(SalesInvoiceDto invoice, string customerFullName, string siteUrl)
    {
        var sb = new StringBuilder();
        var currency = CultureInfo.GetCultureInfo("en-NP");
        string fmt(decimal value) => "Rs. " + value.ToString("N2", currency);

        sb.AppendLine($"Hi {customerFullName},");
        sb.AppendLine();
        sb.AppendLine($"Thanks for shopping with AutoParts. Your invoice {invoice.InvoiceNumber} is attached as a PDF.");
        sb.AppendLine();
        sb.AppendLine($"Invoice number : {invoice.InvoiceNumber}");
        sb.AppendLine($"Issue date     : {invoice.IssueDate:dd MMM yyyy}");
        if (invoice.DueDate.HasValue)
            sb.AppendLine($"Due date       : {invoice.DueDate.Value:dd MMM yyyy}");
        sb.AppendLine();
        sb.AppendLine("Items");
        sb.AppendLine("-----");
        foreach (var item in invoice.Items)
        {
            sb.AppendLine($"{item.PartName} ({item.Sku}) - {item.Quantity} x {fmt(item.UnitPrice)} = {fmt(item.LineTotal)}");
        }
        sb.AppendLine();
        sb.AppendLine($"Subtotal       : {fmt(invoice.Subtotal)}");
        if (invoice.DiscountAmount > 0)
            sb.AppendLine($"Loyalty disc.  : - {fmt(invoice.DiscountAmount)}");
        sb.AppendLine($"Total          : {fmt(invoice.TotalAmount)}");
        if (invoice.AmountPaid > 0)
            sb.AppendLine($"Paid           : {fmt(invoice.AmountPaid)}");
        if (invoice.AmountDue > 0)
            sb.AppendLine($"Balance due    : {fmt(invoice.AmountDue)}");
        sb.AppendLine();
        sb.AppendLine($"View this invoice and your full history online: {siteUrl}/my-history");
        sb.AppendLine();
        sb.AppendLine("AutoParts");

        return sb.ToString();
    }
}
