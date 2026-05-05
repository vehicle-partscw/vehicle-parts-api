using System.Globalization;
using System.Text;

namespace AutoParts.Application.Features.SalesInvoices;

// builds the polite reminder email for an overdue On-Credit invoice.
// keeps the same terra-cotta header look as the regular invoice email so it feels familiar,
// but the tone is "friendly nudge" rather than "here is your bill".
public static class OverdueReminderEmailBuilder
{
    public record EmailContent(string Subject, string HtmlBody, string TextBody);

    public static EmailContent Build(SalesInvoiceDto invoice, string customerFullName, int daysOverdue, string siteUrl)
    {
        var subject = $"Reminder: invoice {invoice.InvoiceNumber} is overdue";

        var html = BuildHtml(invoice, customerFullName, daysOverdue, siteUrl);
        var text = BuildText(invoice, customerFullName, daysOverdue, siteUrl);

        return new EmailContent(subject, html, text);
    }

    private static string BuildHtml(SalesInvoiceDto invoice, string customerFullName, int daysOverdue, string siteUrl)
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
      <div style=""font-size:22px; font-weight:700; margin-top:4px;"">A friendly reminder</div>
    </div>
    <div style=""padding:24px 28px;"">
      <p style=""margin:0 0 12px 0;"">Hi ");
        sb.Append(System.Net.WebUtility.HtmlEncode(customerFullName));
        sb.Append(@",</p>
      <p style=""margin:0 0 16px 0;"">This is just a friendly nudge that invoice <strong>");
        sb.Append(invoice.InvoiceNumber);
        sb.Append(@"</strong> is now <strong>");
        sb.Append(daysOverdue);
        sb.Append(@" day");
        sb.Append(daysOverdue == 1 ? "" : "s");
        sb.Append(@" overdue</strong>. We've attached the original invoice as a PDF for your reference.</p>

      <div style=""background:#FFF1ED; border:1px solid #FFE0D6; border-radius:10px; padding:16px 18px; margin:18px 0;"">
        <div style=""font-size:13px; color:#5C271F; opacity:0.85; margin-bottom:6px;"">AMOUNT STILL DUE</div>
        <div style=""font-size:28px; font-weight:700; color:#E54D2E;"">");
        sb.Append(fmt(invoice.AmountDue));
        sb.Append(@"</div>
        <div style=""font-size:12px; color:#888; margin-top:6px;"">on invoice ");
        sb.Append(invoice.InvoiceNumber);
        sb.Append(@" issued ");
        sb.Append(invoice.IssueDate.ToString("dd MMM yyyy", CultureInfo.InvariantCulture));
        sb.Append(@"</div>
      </div>

      <p style=""margin:0 0 16px 0; font-size:14px;"">Whenever you get a chance, please pop into the shop or transfer the balance to settle the invoice. If the invoice has already been paid, please ignore this message - it can take us a day or two to update our records.</p>

      <p style=""margin:24px 0 8px 0; font-size:13px; color:#5C271F;"">You can also see your full purchase history online:</p>
      <p style=""margin:0 0 24px 0;""><a href=""");
        sb.Append(siteUrl);
        sb.Append(@"/my-history"" style=""display:inline-block; background:#E54D2E; color:#FFFFFF; padding:10px 18px; border-radius:8px; text-decoration:none; font-weight:600; font-size:13px;"">View my history</a></p>

      <p style=""margin:0; font-size:12px; color:#888;"">If you have any questions please reply to this email - we're happy to help.</p>
    </div>
  </div>
</body></html>");

        return sb.ToString();
    }

    private static string BuildText(SalesInvoiceDto invoice, string customerFullName, int daysOverdue, string siteUrl)
    {
        var sb = new StringBuilder();
        var currency = CultureInfo.GetCultureInfo("en-NP");
        string fmt(decimal value) => "Rs. " + value.ToString("N2", currency);

        sb.AppendLine($"Hi {customerFullName},");
        sb.AppendLine();
        sb.AppendLine($"This is a friendly reminder that invoice {invoice.InvoiceNumber} is now {daysOverdue} day{(daysOverdue == 1 ? "" : "s")} overdue.");
        sb.AppendLine($"The original invoice is attached as a PDF for your reference.");
        sb.AppendLine();
        sb.AppendLine($"Amount still due : {fmt(invoice.AmountDue)}");
        sb.AppendLine($"Invoice number   : {invoice.InvoiceNumber}");
        sb.AppendLine($"Issue date       : {invoice.IssueDate:dd MMM yyyy}");
        sb.AppendLine();
        sb.AppendLine("If the invoice has already been paid please ignore this message - it can take us a day or two to update our records.");
        sb.AppendLine();
        sb.AppendLine($"View your purchase history online: {siteUrl}/my-history");
        sb.AppendLine();
        sb.AppendLine("AutoParts");

        return sb.ToString();
    }
}
