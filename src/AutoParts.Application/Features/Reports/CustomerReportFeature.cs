using AutoParts.Application.Common.Interfaces;
using AutoParts.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AutoParts.Application.Features.Reports;

public class CustomerReportRow
{
    public string CustomerUserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public int Count { get; set; }
    public DateOnly? LastDate { get; set; }
}

public class CustomerReportDto
{
    public string Type { get; set; } = string.Empty;
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public string ValueLabel { get; set; } = string.Empty;
    public string CountLabel { get; set; } = string.Empty;
    public List<CustomerReportRow> Rows { get; set; } = new();
}

public static class GetCustomerReport
{
    public class Query : IRequest<CustomerReportDto>
    {
        public string Type { get; set; } = "top-spenders";
        public DateOnly? From { get; set; }
        public DateOnly? To { get; set; }
        public int? Limit { get; set; }
    }

    public class Handler : IRequestHandler<Query, CustomerReportDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IIdentityService _identity;
        public Handler(IApplicationDbContext db, IIdentityService identity)
        {
            _db = db;
            _identity = identity;
        }

        public async Task<CustomerReportDto> Handle(Query req, CancellationToken ct)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var from = req.From ?? today.AddYears(-1);
            var to = req.To ?? today;
            var limit = Math.Clamp(req.Limit ?? 20, 1, 200);
            var type = (req.Type ?? "top-spenders").ToLowerInvariant();

            var customers = await _identity.GetAllCustomersAsync();
            var lookup = customers.ToDictionary(c => c.UserId);

            var sales = await _db.SalesInvoices.AsNoTracking()
                .Where(s => s.IssueDate >= from && s.IssueDate <= to)
                .Select(s => new
                {
                    s.CustomerUserId,
                    s.IssueDate,
                    s.DueDate,
                    s.TotalAmount,
                    s.AmountDue,
                    s.PaymentStatus
                })
                .ToListAsync(ct);

            var rows = type switch
            {
                "regulars" => sales
                    .GroupBy(s => s.CustomerUserId)
                    .Select(g => new CustomerReportRow
                    {
                        CustomerUserId = g.Key,
                        FullName = lookup.TryGetValue(g.Key, out var c) ? c.FullName : "Unknown",
                        Email = lookup.TryGetValue(g.Key, out var c2) ? c2.Email : "",
                        Value = g.Sum(x => x.TotalAmount),
                        Count = g.Count(),
                        LastDate = g.Max(x => x.IssueDate)
                    })
                    .OrderByDescending(r => r.Count)
                    .ThenByDescending(r => r.Value)
                    .Take(limit)
                    .ToList(),

                "overdue" => sales
                    .Where(s =>
                        s.AmountDue > 0 &&
                        (s.PaymentStatus == PaymentStatus.OnCredit ||
                         s.PaymentStatus == PaymentStatus.Unpaid ||
                         s.PaymentStatus == PaymentStatus.PartiallyPaid) &&
                        s.DueDate.HasValue && s.DueDate.Value < today)
                    .GroupBy(s => s.CustomerUserId)
                    .Select(g => new CustomerReportRow
                    {
                        CustomerUserId = g.Key,
                        FullName = lookup.TryGetValue(g.Key, out var c) ? c.FullName : "Unknown",
                        Email = lookup.TryGetValue(g.Key, out var c2) ? c2.Email : "",
                        Value = g.Sum(x => x.AmountDue),
                        Count = g.Count(),
                        LastDate = g.Min(x => x.DueDate)
                    })
                    .OrderByDescending(r => r.Value)
                    .Take(limit)
                    .ToList(),

                _ => sales
                    .GroupBy(s => s.CustomerUserId)
                    .Select(g => new CustomerReportRow
                    {
                        CustomerUserId = g.Key,
                        FullName = lookup.TryGetValue(g.Key, out var c) ? c.FullName : "Unknown",
                        Email = lookup.TryGetValue(g.Key, out var c2) ? c2.Email : "",
                        Value = g.Sum(x => x.TotalAmount),
                        Count = g.Count(),
                        LastDate = g.Max(x => x.IssueDate)
                    })
                    .OrderByDescending(r => r.Value)
                    .Take(limit)
                    .ToList(),
            };

            var (valueLabel, countLabel) = type switch
            {
                "regulars" => ("Total spent", "Visits"),
                "overdue"  => ("Amount overdue", "Open invoices"),
                _          => ("Total spent", "Invoices"),
            };

            return new CustomerReportDto
            {
                Type = type,
                From = from,
                To = to,
                ValueLabel = valueLabel,
                CountLabel = countLabel,
                Rows = rows
            };
        }
    }
}
