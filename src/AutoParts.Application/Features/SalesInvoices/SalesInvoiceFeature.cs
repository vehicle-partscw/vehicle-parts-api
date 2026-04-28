using AutoParts.Application.Common.Exceptions;
using AutoParts.Application.Common.Interfaces;
using AutoParts.Application.Common.Models;
using AutoParts.Application.Common.Security;
using AutoParts.Domain.Common;
using AutoParts.Domain.Entities;
using AutoParts.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AutoParts.Application.Features.SalesInvoices;

public class SalesInvoiceLineDto
{
    public Guid PartId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string PartName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

public class SalesInvoiceDto
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string CustomerUserId { get; set; } = string.Empty;
    public string CreatedByUserId { get; set; } = string.Empty;
    public Guid? LoyaltyTierId { get; set; }
    public string? LoyaltyTierName { get; set; }
    public DateOnly IssueDate { get; set; }
    public DateOnly? DueDate { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal AmountDue { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public List<SalesInvoiceLineDto> Items { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public static class CreateSalesInvoice
{
    public class LineInput
    {
        public Guid PartId { get; set; }
        public int Quantity { get; set; }
    }

    public class Command : IRequest<Guid>
    {
        public string InvoiceNumber { get; set; } = string.Empty;
        public string CustomerUserId { get; set; } = string.Empty;
        public DateOnly IssueDate { get; set; }
        public DateOnly? DueDate { get; set; }
        public Guid? RelatedAppointmentId { get; set; }   // optional — links the invoice back to a Done appointment
        public List<LineInput> Items { get; set; } = new();
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.InvoiceNumber).NotEmpty().MaximumLength(32);
            RuleFor(x => x.CustomerUserId).NotEmpty();
            RuleFor(x => x.Items).NotEmpty();
            RuleForEach(x => x.Items).ChildRules(line =>
            {
                line.RuleFor(l => l.PartId).NotEmpty();
                line.RuleFor(l => l.Quantity).GreaterThan(0);
            });
        }
    }

    public class Handler : IRequestHandler<Command, Guid>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUser _user;
        public Handler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

        public async Task<Guid> Handle(Command req, CancellationToken ct)
        {
            var number = req.InvoiceNumber.Trim();
            if (await _db.SalesInvoices.AnyAsync(s => s.InvoiceNumber == number, ct))
                throw new DomainException($"Invoice number '{number}' already exists.");

            var partIds = req.Items.Select(i => i.PartId).Distinct().ToList();
            var parts = await _db.Parts.Where(p => partIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);

            var lines = new List<SalesInvoiceItem>();
            foreach (var line in req.Items)
            {
                if (!parts.TryGetValue(line.PartId, out var part))
                    throw new NotFoundException("Part", line.PartId);

                if (part.StockQty < line.Quantity)
                    throw new DomainException($"Insufficient stock for SKU '{part.Sku}'. Available: {part.StockQty}.");

                part.StockQty -= line.Quantity;

                lines.Add(new SalesInvoiceItem
                {
                    PartId = part.Id,
                    Quantity = line.Quantity,
                    UnitPrice = part.UnitPrice,
                    LineTotal = part.UnitPrice * line.Quantity
                });
            }

            var subtotal = lines.Sum(l => l.LineTotal);

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var tier = await _db.LoyaltyTiers
                .Where(t => t.IsActive
                            && t.MinSinglePurchase <= subtotal
                            && t.ValidFrom <= today
                            && (t.ValidTo == null || t.ValidTo >= today))
                .OrderByDescending(t => t.MinSinglePurchase)
                .FirstOrDefaultAsync(ct);

            decimal discount = 0;
            if (tier is not null)
                discount = Math.Round(subtotal * tier.DiscountPercent / 100m, 2);

            var total = subtotal - discount;

            // if linked to an appointment, validate it belongs to the same customer and is Done
            if (req.RelatedAppointmentId.HasValue)
            {
                var appt = await _db.Appointments
                    .FirstOrDefaultAsync(a => a.Id == req.RelatedAppointmentId.Value, ct);
                if (appt is null)
                    throw new NotFoundException("Appointment", req.RelatedAppointmentId.Value);
                if (appt.CustomerUserId != req.CustomerUserId)
                    throw new DomainException("The appointment belongs to a different customer.");
            }

            var invoice = new SalesInvoice
            {
                InvoiceNumber = number,
                CustomerUserId = req.CustomerUserId,
                CreatedByUserId = _user.UserId ?? string.Empty,
                LoyaltyTierId = tier?.Id,
                RelatedAppointmentId = req.RelatedAppointmentId,
                IssueDate = req.IssueDate,
                DueDate = req.DueDate,
                Subtotal = subtotal,
                DiscountAmount = discount,
                TotalAmount = total,
                AmountPaid = 0,
                AmountDue = total,
                PaymentStatus = PaymentStatus.Unpaid,
                Items = lines
            };

            _db.SalesInvoices.Add(invoice);
            await _db.SaveChangesAsync(ct);
            return invoice.Id;
        }
    }
}

public static class GetSalesInvoiceById
{
    public class Query : IRequest<SalesInvoiceDto?> { public Guid Id { get; set; } }

    public class Handler : IRequestHandler<Query, SalesInvoiceDto?>
    {
        private readonly IApplicationDbContext _db;
        public Handler(IApplicationDbContext db) { _db = db; }
        public Task<SalesInvoiceDto?> Handle(Query req, CancellationToken ct) =>
            _db.SalesInvoices
                .Include(s => s.LoyaltyTier)
                .Include(s => s.Items).ThenInclude(i => i.Part)
                .Where(s => s.Id == req.Id)
                .Select(s => new SalesInvoiceDto
                {
                    Id = s.Id,
                    InvoiceNumber = s.InvoiceNumber,
                    CustomerUserId = s.CustomerUserId,
                    CreatedByUserId = s.CreatedByUserId,
                    LoyaltyTierId = s.LoyaltyTierId,
                    LoyaltyTierName = s.LoyaltyTier != null ? s.LoyaltyTier.Name : null,
                    IssueDate = s.IssueDate,
                    DueDate = s.DueDate,
                    Subtotal = s.Subtotal,
                    DiscountAmount = s.DiscountAmount,
                    TotalAmount = s.TotalAmount,
                    AmountPaid = s.AmountPaid,
                    AmountDue = s.AmountDue,
                    PaymentStatus = s.PaymentStatus.ToString(),
                    Items = s.Items.Select(i => new SalesInvoiceLineDto
                    {
                        PartId = i.PartId,
                        Sku = i.Part != null ? i.Part.Sku : string.Empty,
                        PartName = i.Part != null ? i.Part.Name : string.Empty,
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice,
                        LineTotal = i.LineTotal
                    }).ToList(),
                    CreatedAt = s.CreatedAt
                })
                .FirstOrDefaultAsync(ct);
    }
}

public static class GetSalesInvoices
{
    public class Query : PaginationParams, IRequest<PaginatedList<SalesInvoiceDto>>
    {
        public string? CustomerUserId { get; set; }
        public PaymentStatus? Status { get; set; }
        public DateOnly? From { get; set; }
        public DateOnly? To { get; set; }
        public bool? Overdue { get; set; }
    }

    public class Handler : IRequestHandler<Query, PaginatedList<SalesInvoiceDto>>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUser _user;
        public Handler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

        public Task<PaginatedList<SalesInvoiceDto>> Handle(Query req, CancellationToken ct)
        {
            var q = _db.SalesInvoices.Include(s => s.LoyaltyTier).AsNoTracking();

            if (_user.IsInRole(Roles.Customer))
                q = q.Where(s => s.CustomerUserId == _user.UserId);
            else if (!string.IsNullOrWhiteSpace(req.CustomerUserId))
                q = q.Where(s => s.CustomerUserId == req.CustomerUserId);

            if (req.Status.HasValue) q = q.Where(s => s.PaymentStatus == req.Status.Value);
            if (req.From.HasValue) q = q.Where(s => s.IssueDate >= req.From.Value);
            if (req.To.HasValue) q = q.Where(s => s.IssueDate <= req.To.Value);
            if (req.Overdue == true)
            {
                var cutoff = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-1);
                q = q.Where(s => s.AmountDue > 0 && s.IssueDate < cutoff);
            }

            var dtos = q.OrderByDescending(s => s.IssueDate).Select(s => new SalesInvoiceDto
            {
                Id = s.Id,
                InvoiceNumber = s.InvoiceNumber,
                CustomerUserId = s.CustomerUserId,
                CreatedByUserId = s.CreatedByUserId,
                LoyaltyTierId = s.LoyaltyTierId,
                LoyaltyTierName = s.LoyaltyTier != null ? s.LoyaltyTier.Name : null,
                IssueDate = s.IssueDate,
                DueDate = s.DueDate,
                Subtotal = s.Subtotal,
                DiscountAmount = s.DiscountAmount,
                TotalAmount = s.TotalAmount,
                AmountPaid = s.AmountPaid,
                AmountDue = s.AmountDue,
                PaymentStatus = s.PaymentStatus.ToString(),
                CreatedAt = s.CreatedAt
            });
            return PaginatedList<SalesInvoiceDto>.CreateAsync(dtos, req.Page, req.PageSize, ct);
        }
    }
}
