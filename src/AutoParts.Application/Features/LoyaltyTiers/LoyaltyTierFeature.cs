using AutoParts.Application.Common.Exceptions;
using AutoParts.Application.Common.Interfaces;
using AutoParts.Application.Common.Models;
using AutoParts.Domain.Common;
using AutoParts.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AutoParts.Application.Features.LoyaltyTiers;

public class LoyaltyTierDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal MinSinglePurchase { get; set; }
    public decimal DiscountPercent { get; set; }
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
    public bool IsActive { get; set; }
}

public static class CreateLoyaltyTier
{
    public class Command : IRequest<Guid>
    {
        public string Name { get; set; } = string.Empty;
        public decimal MinSinglePurchase { get; set; }
        public decimal DiscountPercent { get; set; }
        public DateOnly ValidFrom { get; set; }
        public DateOnly? ValidTo { get; set; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
            RuleFor(x => x.MinSinglePurchase).GreaterThanOrEqualTo(0);
            RuleFor(x => x.DiscountPercent).InclusiveBetween(0m, 100m);
        }
    }

    public class Handler : IRequestHandler<Command, Guid>
    {
        private readonly IApplicationDbContext _db;
        public Handler(IApplicationDbContext db) { _db = db; }
        public async Task<Guid> Handle(Command req, CancellationToken ct)
        {
            if (await _db.LoyaltyTiers.AnyAsync(t => t.Name == req.Name.Trim(), ct))
                throw new DomainException($"Loyalty tier '{req.Name}' already exists.");

            var tier = new LoyaltyTier
            {
                Name = req.Name.Trim(),
                MinSinglePurchase = req.MinSinglePurchase,
                DiscountPercent = req.DiscountPercent,
                ValidFrom = req.ValidFrom,
                ValidTo = req.ValidTo,
                IsActive = true
            };
            _db.LoyaltyTiers.Add(tier);
            await _db.SaveChangesAsync(ct);
            return tier.Id;
        }
    }
}

public static class UpdateLoyaltyTier
{
    public class Command : IRequest<Unit>
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal MinSinglePurchase { get; set; }
        public decimal DiscountPercent { get; set; }
        public DateOnly ValidFrom { get; set; }
        public DateOnly? ValidTo { get; set; }
        public bool IsActive { get; set; }
    }

    public class Handler : IRequestHandler<Command, Unit>
    {
        private readonly IApplicationDbContext _db;
        public Handler(IApplicationDbContext db) { _db = db; }
        public async Task<Unit> Handle(Command req, CancellationToken ct)
        {
            var t = await _db.LoyaltyTiers.FirstOrDefaultAsync(x => x.Id == req.Id, ct)
                ?? throw new NotFoundException("LoyaltyTier", req.Id);
            t.Name = req.Name.Trim();
            t.MinSinglePurchase = req.MinSinglePurchase;
            t.DiscountPercent = req.DiscountPercent;
            t.ValidFrom = req.ValidFrom;
            t.ValidTo = req.ValidTo;
            t.IsActive = req.IsActive;
            await _db.SaveChangesAsync(ct);
            return Unit.Value;
        }
    }
}

public static class DeleteLoyaltyTier
{
    public class Command : IRequest<Unit> { public Guid Id { get; set; } }
    public class Handler : IRequestHandler<Command, Unit>
    {
        private readonly IApplicationDbContext _db;
        public Handler(IApplicationDbContext db) { _db = db; }
        public async Task<Unit> Handle(Command req, CancellationToken ct)
        {
            var t = await _db.LoyaltyTiers.FirstOrDefaultAsync(x => x.Id == req.Id, ct)
                ?? throw new NotFoundException("LoyaltyTier", req.Id);
            _db.LoyaltyTiers.Remove(t);
            await _db.SaveChangesAsync(ct);
            return Unit.Value;
        }
    }
}

public static class GetLoyaltyTiers
{
    public class Query : PaginationParams, IRequest<PaginatedList<LoyaltyTierDto>> { }

    public class Handler : IRequestHandler<Query, PaginatedList<LoyaltyTierDto>>
    {
        private readonly IApplicationDbContext _db;
        public Handler(IApplicationDbContext db) { _db = db; }
        public Task<PaginatedList<LoyaltyTierDto>> Handle(Query req, CancellationToken ct)
        {
            var q = _db.LoyaltyTiers.AsNoTracking()
                .OrderByDescending(t => t.MinSinglePurchase)
                .Select(t => new LoyaltyTierDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    MinSinglePurchase = t.MinSinglePurchase,
                    DiscountPercent = t.DiscountPercent,
                    ValidFrom = t.ValidFrom,
                    ValidTo = t.ValidTo,
                    IsActive = t.IsActive
                });
            return PaginatedList<LoyaltyTierDto>.CreateAsync(q, req.Page, req.PageSize, ct);
        }
    }
}
