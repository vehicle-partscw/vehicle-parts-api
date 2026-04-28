using AutoParts.Application.Common.Exceptions;
using AutoParts.Application.Common.Interfaces;
using AutoParts.Application.Common.Models;
using AutoParts.Domain.Common;
using AutoParts.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AutoParts.Application.Features.Parts;

public class PartDto
{
    public Guid Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public Guid VendorId { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int StockQty { get; set; }
    public short ReorderLevel { get; set; }
    public bool IsLowStock => StockQty < ReorderLevel;
    public string? ImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}

public static class CreatePart
{
    public class Command : IRequest<Guid>
    {
        public string Sku { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid CategoryId { get; set; }
        public Guid VendorId { get; set; }
        public decimal UnitPrice { get; set; }
        public int StockQty { get; set; }
        public short ReorderLevel { get; set; } = 10;
        public string? ImageUrl { get; set; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Sku).NotEmpty().MaximumLength(40)
                .Matches("^[A-Z0-9-]+$").WithMessage("SKU must be uppercase letters, digits or hyphens.");
            RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
            RuleFor(x => x.CategoryId).NotEmpty();
            RuleFor(x => x.VendorId).NotEmpty();
            RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
            RuleFor(x => x.StockQty).GreaterThanOrEqualTo(0);
            RuleFor(x => x.ReorderLevel).GreaterThanOrEqualTo((short)0);
        }
    }

    public class Handler : IRequestHandler<Command, Guid>
    {
        private readonly IApplicationDbContext _db;
        public Handler(IApplicationDbContext db) { _db = db; }

        public async Task<Guid> Handle(Command request, CancellationToken ct)
        {
            var sku = request.Sku.Trim().ToUpperInvariant();
            // IgnoreQueryFilters so a soft-deleted part with this SKU is still considered taken
            // (the unique index in Postgres covers all rows, deleted or not)
            if (await _db.Parts.IgnoreQueryFilters().AnyAsync(p => p.Sku == sku, ct))
                throw new DomainException($"SKU '{sku}' is already in use.");

            if (!await _db.PartCategories.AnyAsync(c => c.Id == request.CategoryId, ct))
                throw new NotFoundException("PartCategory", request.CategoryId);
            if (!await _db.Vendors.AnyAsync(v => v.Id == request.VendorId, ct))
                throw new NotFoundException("Vendor", request.VendorId);

            var part = new Part
            {
                Sku = sku,
                Name = request.Name.Trim(),
                Description = request.Description?.Trim(),
                CategoryId = request.CategoryId,
                VendorId = request.VendorId,
                UnitPrice = request.UnitPrice,
                StockQty = request.StockQty,
                ReorderLevel = request.ReorderLevel,
                ImageUrl = request.ImageUrl
            };
            _db.Parts.Add(part);
            try
            {
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                // Safety net: even if the pre-check missed it (race / case mismatch / new unique col)
                // we translate Postgres unique-violation 23505 to a friendly domain error
                throw new DomainException($"SKU '{sku}' is already in use.");
            }
            return part.Id;
        }

        private static bool IsUniqueViolation(DbUpdateException ex)
        {
            // Avoid taking a Npgsql dependency in Application; PostgresException stores SqlState in Data
            var inner = ex.InnerException;
            if (inner is null) return false;
            var state = inner.Data["SqlState"] as string;
            return state == "23505";
        }
    }
}

public static class UpdatePart
{
    public class Command : IRequest<Unit>
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid CategoryId { get; set; }
        public Guid VendorId { get; set; }
        public decimal UnitPrice { get; set; }
        public short ReorderLevel { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
            RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
        }
    }

    public class Handler : IRequestHandler<Command, Unit>
    {
        private readonly IApplicationDbContext _db;
        public Handler(IApplicationDbContext db) { _db = db; }

        public async Task<Unit> Handle(Command request, CancellationToken ct)
        {
            var part = await _db.Parts.FirstOrDefaultAsync(p => p.Id == request.Id, ct)
                ?? throw new NotFoundException("Part", request.Id);

            part.Name = request.Name.Trim();
            part.Description = request.Description?.Trim();
            part.CategoryId = request.CategoryId;
            part.VendorId = request.VendorId;
            part.UnitPrice = request.UnitPrice;
            part.ReorderLevel = request.ReorderLevel;
            part.ImageUrl = request.ImageUrl;
            await _db.SaveChangesAsync(ct);
            return Unit.Value;
        }
    }
}

public static class DeletePart
{
    public class Command : IRequest<Unit> { public Guid Id { get; set; } }

    public class Handler : IRequestHandler<Command, Unit>
    {
        private readonly IApplicationDbContext _db;
        public Handler(IApplicationDbContext db) { _db = db; }
        public async Task<Unit> Handle(Command request, CancellationToken ct)
        {
            var part = await _db.Parts.FirstOrDefaultAsync(p => p.Id == request.Id, ct)
                ?? throw new NotFoundException("Part", request.Id);
            _db.Parts.Remove(part);
            await _db.SaveChangesAsync(ct);
            return Unit.Value;
        }
    }
}

public static class GetPartById
{
    public class Query : IRequest<PartDto?> { public Guid Id { get; set; } }

    public class Handler : IRequestHandler<Query, PartDto?>
    {
        private readonly IApplicationDbContext _db;
        public Handler(IApplicationDbContext db) { _db = db; }
        public Task<PartDto?> Handle(Query request, CancellationToken ct) =>
            _db.Parts
                .Include(p => p.Category)
                .Include(p => p.Vendor)
                .Where(p => p.Id == request.Id)
                .Select(p => Project(p))
                .FirstOrDefaultAsync(ct);
    }

    internal static PartDto Project(Part p) => new()
    {
        Id = p.Id,
        Sku = p.Sku,
        Name = p.Name,
        Description = p.Description,
        CategoryId = p.CategoryId,
        CategoryName = p.Category != null ? p.Category.Name : string.Empty,
        VendorId = p.VendorId,
        VendorName = p.Vendor != null ? p.Vendor.Name : string.Empty,
        UnitPrice = p.UnitPrice,
        StockQty = p.StockQty,
        ReorderLevel = p.ReorderLevel,
        ImageUrl = p.ImageUrl,
        CreatedAt = p.CreatedAt
    };
}

public static class GetParts
{
    public class Query : PaginationParams, IRequest<PaginatedList<PartDto>>
    {
        public Guid? CategoryId { get; set; }
        public Guid? VendorId { get; set; }
        public bool? LowStock { get; set; }
    }

    public class Handler : IRequestHandler<Query, PaginatedList<PartDto>>
    {
        private readonly IApplicationDbContext _db;
        public Handler(IApplicationDbContext db) { _db = db; }
        public Task<PaginatedList<PartDto>> Handle(Query req, CancellationToken ct)
        {
            var q = _db.Parts.Include(p => p.Category).Include(p => p.Vendor).AsNoTracking();

            if (!string.IsNullOrWhiteSpace(req.Search))
            {
                var s = req.Search.Trim().ToLower();
                q = q.Where(p => p.Sku.ToLower().Contains(s) || p.Name.ToLower().Contains(s));
            }
            if (req.CategoryId.HasValue) q = q.Where(p => p.CategoryId == req.CategoryId.Value);
            if (req.VendorId.HasValue) q = q.Where(p => p.VendorId == req.VendorId.Value);
            if (req.LowStock == true) q = q.Where(p => p.StockQty < p.ReorderLevel);

            var dtos = q.OrderBy(p => p.Name).Select(p => GetPartById.Project(p));
            return PaginatedList<PartDto>.CreateAsync(dtos, req.Page, req.PageSize, ct);
        }
    }
}
