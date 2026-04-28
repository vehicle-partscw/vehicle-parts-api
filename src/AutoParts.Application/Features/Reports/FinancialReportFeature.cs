using AutoParts.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AutoParts.Application.Features.Reports;

public class TimeBucketDto
{
    public string Label { get; set; } = string.Empty;
    public decimal Sales { get; set; }
    public decimal Purchases { get; set; }
}

public class TopPartDto
{
    public Guid PartId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal Revenue { get; set; }
}

public class TopCustomerDto
{
    public string CustomerUserId { get; set; } = string.Empty;
    public int InvoiceCount { get; set; }
    public decimal TotalSpent { get; set; }
}

public class FinancialReportDto
{
    public string Period { get; set; } = string.Empty;
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public decimal TotalSales { get; set; }
    public decimal TotalPurchases { get; set; }
    public decimal GrossMargin { get; set; }
    public int SalesInvoiceCount { get; set; }
    public int PurchaseInvoiceCount { get; set; }
    public List<TimeBucketDto> Series { get; set; } = new();
    public List<TopPartDto> TopParts { get; set; } = new();
    public List<TopCustomerDto> TopCustomers { get; set; } = new();
}

public static class GetFinancialReport
{
    public class Query : IRequest<FinancialReportDto>
    {
        public string Period { get; set; } = "monthly";
        public DateOnly? From { get; set; }
        public DateOnly? To { get; set; }
    }

    public class Handler : IRequestHandler<Query, FinancialReportDto>
    {
        private readonly IApplicationDbContext _db;
        public Handler(IApplicationDbContext db) { _db = db; }

        public async Task<FinancialReportDto> Handle(Query req, CancellationToken ct)
        {
            var period = (req.Period ?? "monthly").ToLower();
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var (from, to) = (req.From, req.To) switch
            {
                ({ } f, { } t) => (f, t),
                ({ } f, null) => (f, today),
                (null, { } t) => (t.AddYears(-1), t),
                _ => period switch
                {
                    "daily"   => (today.AddDays(-29), today),
                    "yearly"  => (today.AddYears(-4), today),
                    _         => (today.AddMonths(-11), today)
                }
            };

            var sales = await _db.SalesInvoices.AsNoTracking()
                .Where(s => s.IssueDate >= from && s.IssueDate <= to)
                .Select(s => new { s.Id, s.IssueDate, s.TotalAmount, s.CustomerUserId })
                .ToListAsync(ct);

            var purchases = await _db.PurchaseInvoices.AsNoTracking()
                .Where(p => p.IssueDate >= from && p.IssueDate <= to)
                .Select(p => new { p.IssueDate, p.TotalAmount })
                .ToListAsync(ct);

            string Bucket(DateOnly d) => period switch
            {
                "daily"  => d.ToString("yyyy-MM-dd"),
                "yearly" => d.Year.ToString(),
                _        => d.ToString("yyyy-MM")
            };

            var salesByBucket = sales
                .GroupBy(s => Bucket(s.IssueDate))
                .ToDictionary(g => g.Key, g => g.Sum(x => x.TotalAmount));

            var purchasesByBucket = purchases
                .GroupBy(p => Bucket(p.IssueDate))
                .ToDictionary(g => g.Key, g => g.Sum(x => x.TotalAmount));

            var allBuckets = salesByBucket.Keys.Union(purchasesByBucket.Keys)
                .OrderBy(k => k)
                .ToList();

            var series = allBuckets.Select(b => new TimeBucketDto
            {
                Label = b,
                Sales = salesByBucket.GetValueOrDefault(b),
                Purchases = purchasesByBucket.GetValueOrDefault(b)
            }).ToList();

            var topParts = await _db.SalesInvoiceItems.AsNoTracking()
                .Where(i => sales.Select(s => s.Id).Contains(i.SalesInvoiceId))
                .GroupBy(i => i.PartId)
                .Select(g => new
                {
                    PartId = g.Key,
                    Quantity = g.Sum(x => x.Quantity),
                    Revenue = g.Sum(x => x.LineTotal)
                })
                .OrderByDescending(x => x.Revenue)
                .Take(5)
                .ToListAsync(ct);

            var topPartIds = topParts.Select(t => t.PartId).ToList();
            var partLookup = await _db.Parts.AsNoTracking()
                .Where(p => topPartIds.Contains(p.Id))
                .Select(p => new { p.Id, p.Sku, p.Name })
                .ToDictionaryAsync(p => p.Id, ct);

            var topPartsDto = topParts.Select(t => new TopPartDto
            {
                PartId = t.PartId,
                Sku = partLookup.TryGetValue(t.PartId, out var p) ? p.Sku : "?",
                Name = partLookup.TryGetValue(t.PartId, out var p2) ? p2.Name : "?",
                QuantitySold = t.Quantity,
                Revenue = t.Revenue
            }).ToList();

            var topCustomersDto = sales
                .GroupBy(s => s.CustomerUserId)
                .Select(g => new TopCustomerDto
                {
                    CustomerUserId = g.Key,
                    InvoiceCount = g.Count(),
                    TotalSpent = g.Sum(x => x.TotalAmount)
                })
                .OrderByDescending(c => c.TotalSpent)
                .Take(5)
                .ToList();

            return new FinancialReportDto
            {
                Period = period,
                From = from,
                To = to,
                TotalSales = sales.Sum(s => s.TotalAmount),
                TotalPurchases = purchases.Sum(p => p.TotalAmount),
                GrossMargin = sales.Sum(s => s.TotalAmount) - purchases.Sum(p => p.TotalAmount),
                SalesInvoiceCount = sales.Count,
                PurchaseInvoiceCount = purchases.Count,
                Series = series,
                TopParts = topPartsDto,
                TopCustomers = topCustomersDto
            };
        }
    }
}
