using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AutoParts.Application.Features.SalesInvoices;

// renders a sales invoice into an a4 pdf using QuestPDF.
// the pdf is meant to be attached to the invoice email.
public static class InvoicePdfBuilder
{
    private static readonly string Brand = "#E54D2E";
    private static readonly string Ink = "#1A0F0C";
    private static readonly string Soft = "#FFE7E0";
    private static readonly string Muted = "#888888";

    public static byte[] Build(SalesInvoiceDto invoice, string customerFullName)
    {
        var currency = CultureInfo.GetCultureInfo("en-NP");
        string money(decimal v) => "Rs. " + v.ToString("N2", currency);

        return Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(t => t.FontSize(10).FontColor(Ink));

                page.Header().Element(h =>
                {
                    h.Background(Brand).PaddingVertical(18).PaddingHorizontal(24).Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("AUTOPARTS").FontSize(11).LetterSpacing(2).FontColor(Colors.White);
                            col.Item().PaddingTop(2).Text("Invoice").FontSize(20).Bold().FontColor(Colors.White);
                        });
                        row.ConstantItem(170).AlignRight().Column(col =>
                        {
                            col.Item().Text(invoice.InvoiceNumber).FontSize(13).Bold().FontColor(Colors.White);
                            col.Item().Text(invoice.IssueDate.ToString("dd MMM yyyy", CultureInfo.InvariantCulture))
                                .FontSize(10).FontColor(Colors.White);
                        });
                    });
                });

                page.Content().PaddingVertical(20).Column(content =>
                {
                    content.Item().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("BILL TO").FontSize(9).LetterSpacing(1).FontColor(Muted);
                            col.Item().PaddingTop(2).Text(customerFullName).FontSize(12).SemiBold();
                        });
                        row.ConstantItem(170).AlignRight().Column(col =>
                        {
                            col.Item().Text("ISSUE DATE").FontSize(9).LetterSpacing(1).FontColor(Muted);
                            col.Item().PaddingTop(2).Text(invoice.IssueDate.ToString("dd MMM yyyy", CultureInfo.InvariantCulture)).SemiBold();
                            if (invoice.DueDate.HasValue)
                            {
                                col.Item().PaddingTop(8).Text("DUE DATE").FontSize(9).LetterSpacing(1).FontColor(Muted);
                                col.Item().PaddingTop(2).Text(invoice.DueDate.Value.ToString("dd MMM yyyy", CultureInfo.InvariantCulture)).SemiBold();
                            }
                        });
                    });

                    content.Item().PaddingTop(24).Element(BuildItemsTable);

                    content.Item().PaddingTop(18).AlignRight().Width(260).Column(totals =>
                    {
                        totals.Item().Element(t => TotalsRow(t, "Subtotal", money(invoice.Subtotal), bold: false));
                        if (invoice.DiscountAmount > 0)
                        {
                            var label = string.IsNullOrEmpty(invoice.LoyaltyTierName)
                                ? "Loyalty discount"
                                : $"Loyalty discount ({invoice.LoyaltyTierName})";
                            totals.Item().Element(t => TotalsRow(t, label, "- " + money(invoice.DiscountAmount), bold: false, color: "#2E7D32"));
                        }
                        totals.Item().PaddingTop(4).BorderTop(1).BorderColor("#E5D6CE").PaddingTop(8)
                            .Element(t => TotalsRow(t, "Total", money(invoice.TotalAmount), bold: true, big: true));
                        if (invoice.AmountPaid > 0)
                            totals.Item().Element(t => TotalsRow(t, "Paid", money(invoice.AmountPaid), bold: false));
                        if (invoice.AmountDue > 0)
                            totals.Item().Element(t => TotalsRow(t, "Balance due", money(invoice.AmountDue), bold: true, color: Brand));
                    });
                });

                page.Footer().PaddingTop(8).Row(row =>
                {
                    row.RelativeItem().Text("Thank you for your business.").FontSize(9).FontColor(Muted);
                    row.ConstantItem(180).AlignRight().Text(t =>
                    {
                        t.Span("AutoParts • ").FontSize(9).FontColor(Muted);
                        t.Span(invoice.InvoiceNumber).FontSize(9).FontColor(Muted).SemiBold();
                    });
                });

                void BuildItemsTable(IContainer container)
                {
                    container.Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(4);
                            c.RelativeColumn(1);
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                        });

                        table.Header(h =>
                        {
                            h.Cell().Background(Soft).PaddingHorizontal(8).PaddingVertical(6).Text("ITEM").FontColor("#5C271F").FontSize(9).LetterSpacing(1);
                            h.Cell().Background(Soft).PaddingHorizontal(8).PaddingVertical(6).AlignCenter().Text("QTY").FontColor("#5C271F").FontSize(9).LetterSpacing(1);
                            h.Cell().Background(Soft).PaddingHorizontal(8).PaddingVertical(6).AlignRight().Text("UNIT PRICE").FontColor("#5C271F").FontSize(9).LetterSpacing(1);
                            h.Cell().Background(Soft).PaddingHorizontal(8).PaddingVertical(6).AlignRight().Text("LINE TOTAL").FontColor("#5C271F").FontSize(9).LetterSpacing(1);
                        });

                        foreach (var item in invoice.Items)
                        {
                            table.Cell().BorderBottom(0.5f).BorderColor("#F1E7E0").PaddingHorizontal(8).PaddingVertical(6)
                                .Text(t =>
                                {
                                    t.Span(item.PartName).FontSize(10);
                                    t.Span($"  ({item.Sku})").FontSize(9).FontColor(Muted);
                                });
                            table.Cell().BorderBottom(0.5f).BorderColor("#F1E7E0").PaddingHorizontal(8).PaddingVertical(6).AlignCenter().Text(item.Quantity.ToString());
                            table.Cell().BorderBottom(0.5f).BorderColor("#F1E7E0").PaddingHorizontal(8).PaddingVertical(6).AlignRight().Text(money(item.UnitPrice));
                            table.Cell().BorderBottom(0.5f).BorderColor("#F1E7E0").PaddingHorizontal(8).PaddingVertical(6).AlignRight().Text(money(item.LineTotal));
                        }
                    });
                }

                static void TotalsRow(IContainer container, string label, string value, bool bold, bool big = false, string? color = null)
                {
                    container.Row(row =>
                    {
                        row.RelativeItem().Text(t =>
                        {
                            var span = t.Span(label).FontSize(big ? 12 : 10);
                            if (bold) span.SemiBold();
                            if (color != null) span.FontColor(color);
                        });
                        row.ConstantItem(110).AlignRight().Text(t =>
                        {
                            var span = t.Span(value).FontSize(big ? 13 : 10);
                            if (bold) span.Bold();
                            if (color != null) span.FontColor(color);
                        });
                    });
                }
            });
        }).GeneratePdf();
    }
}
