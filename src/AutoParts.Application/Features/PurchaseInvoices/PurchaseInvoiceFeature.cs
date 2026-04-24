using AutoParts.Application.Common.Exceptions;
using AutoParts.Application.Common.Interfaces;
using AutoParts.Application.Common.Models;
using AutoParts.Domain.Common;
using AutoParts.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AutoParts.Application.Features.PurchaseInvoices;

public class PurchaseInvoiceLineDto
{
    public Guid PartId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string PartName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal LineTotal { get; set; }
}

public class PurchaseInvoiceDto
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public Guid VendorId { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public string CreatedByUserId { get; set; } = string.Empty;
    public DateOnly IssueDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }
    public List<PurchaseInvoiceLineDto> Items { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public static class CreatePurchaseInvoice
{
    public class LineInput
    {
        public Guid PartId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitCost { get; set; }
    }

    public class Command : IRequest<Guid>
    {
        public string InvoiceNumber { get; set; } = string.Empty;
        public Guid VendorId { get; set; }
        public DateOnly IssueDate { get; set; }
        public string? Notes { get; set; }
        public List<LineInput> Items { get; set; } = new();
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.InvoiceNumber).NotEmpty().MaximumLength(32);
            RuleFor(x => x.VendorId).NotEmpty();
            RuleFor(x => x.Items).NotEmpty().WithMessage("At least one line item is required.");
            RuleForEach(x => x.Items).ChildRules(line =>
            {
                line.RuleFor(l => l.PartId).NotEmpty();
                line.RuleFor(l => l.Quantity).GreaterThan(0);
                line.RuleFor(l => l.UnitCost).GreaterThanOrEqualTo(0);
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
            if (await _db.PurchaseInvoices.AnyAsync(p => p.InvoiceNumber == number, ct))
                throw new DomainException($"Invoice number '{number}' already exists.");

            if (!await _db.Vendors.AnyAsync(v => v.Id == req.VendorId, ct))
                throw new NotFoundException("Vendor", req.VendorId);

            var partIds = req.Items.Select(i => i.PartId).Distinct().ToList();
            var parts = await _db.Parts.Where(p => partIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, ct);

            foreach (var pid in partIds)
            {
                if (!parts.ContainsKey(pid))
                    throw new NotFoundException("Part", pid);
            }

            var invoice = new PurchaseInvoice
            {
                InvoiceNumber = number,
                VendorId = req.VendorId,
                CreatedByUserId = _user.UserId ?? string.Empty,
                IssueDate = req.IssueDate,
                Notes = req.Notes?.Trim(),
                Items = req.Items.Select(line => new PurchaseInvoiceItem
                {
                    PartId = line.PartId,
                    Quantity = line.Quantity,
                    UnitCost = line.UnitCost,
                    LineTotal = line.Quantity * line.UnitCost
                }).ToList()
            };
            invoice.TotalAmount = invoice.Items.Sum(i => i.LineTotal);

            foreach (var line in req.Items)
            {
                parts[line.PartId].StockQty += line.Quantity;
            }

            _db.PurchaseInvoices.Add(invoice);
            await _db.SaveChangesAsync(ct);
            return invoice.Id;
        }
    }
}

public static class GetPurchaseInvoiceById
{
    public class Query : IRequest<PurchaseInvoiceDto?> { public Guid Id { get; set; } }

    public class Handler : IRequestHandler<Query, PurchaseInvoiceDto?>
    {
        private readonly IApplicationDbContext _db;
        public Handler(IApplicationDbContext db) { _db = db; }
        public Task<PurchaseInvoiceDto?> Handle(Query req, CancellationToken ct) =>
            _db.PurchaseInvoices
                .Include(p => p.Vendor)
                .Include(p => p.Items).ThenInclude(i => i.Part)
                .Where(p => p.Id == req.Id)
                .Select(p => new PurchaseInvoiceDto
                {
                    Id = p.Id,
                    InvoiceNumber = p.InvoiceNumber,
                    VendorId = p.VendorId,
                    VendorName = p.Vendor != null ? p.Vendor.Name : string.Empty,
                    CreatedByUserId = p.CreatedByUserId,
                    IssueDate = p.IssueDate,
                    TotalAmount = p.TotalAmount,
                    Notes = p.Notes,
                    Items = p.Items.Select(i => new PurchaseInvoiceLineDto
                    {
                        PartId = i.PartId,
                        Sku = i.Part != null ? i.Part.Sku : string.Empty,
                        PartName = i.Part != null ? i.Part.Name : string.Empty,
                        Quantity = i.Quantity,
                        UnitCost = i.UnitCost,
                        LineTotal = i.LineTotal
                    }).ToList(),
                    CreatedAt = p.CreatedAt
                })
                .FirstOrDefaultAsync(ct);
    }
}

public static class GetPurchaseInvoices
{
    public class Query : PaginationParams, IRequest<PaginatedList<PurchaseInvoiceDto>>
    {
        public Guid? VendorId { get; set; }
        public DateOnly? From { get; set; }
        public DateOnly? To { get; set; }
    }

    public class Handler : IRequestHandler<Query, PaginatedList<PurchaseInvoiceDto>>
    {
        private readonly IApplicationDbContext _db;
        public Handler(IApplicationDbContext db) { _db = db; }
        public Task<PaginatedList<PurchaseInvoiceDto>> Handle(Query req, CancellationToken ct)
        {
            var q = _db.PurchaseInvoices.Include(p => p.Vendor).AsNoTracking();
            if (req.VendorId.HasValue) q = q.Where(p => p.VendorId == req.VendorId.Value);
            if (req.From.HasValue) q = q.Where(p => p.IssueDate >= req.From.Value);
            if (req.To.HasValue) q = q.Where(p => p.IssueDate <= req.To.Value);

            var dtos = q.OrderByDescending(p => p.IssueDate).Select(p => new PurchaseInvoiceDto
            {
                Id = p.Id,
                InvoiceNumber = p.InvoiceNumber,
                VendorId = p.VendorId,
                VendorName = p.Vendor != null ? p.Vendor.Name : string.Empty,
                CreatedByUserId = p.CreatedByUserId,
                IssueDate = p.IssueDate,
                TotalAmount = p.TotalAmount,
                Notes = p.Notes,
                CreatedAt = p.CreatedAt
            });
            return PaginatedList<PurchaseInvoiceDto>.CreateAsync(dtos, req.Page, req.PageSize, ct);
        }
    }
}
