using AutoParts.Application.Common.Exceptions;
using AutoParts.Application.Common.Interfaces;
using AutoParts.Application.Common.Models;
using AutoParts.Domain.Common;
using AutoParts.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AutoParts.Application.Features.ServiceTypes;

public class ServiceTypeDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal BasePrice { get; set; }
    public short EstimatedMinutes { get; set; }
    public bool IsActive { get; set; }
}

public static class CreateServiceType
{
    public class Command : IRequest<Guid>
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal BasePrice { get; set; }
        public short EstimatedMinutes { get; set; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
            RuleFor(x => x.BasePrice).GreaterThanOrEqualTo(0);
            RuleFor(x => x.EstimatedMinutes).GreaterThan((short)0);
        }
    }

    public class Handler : IRequestHandler<Command, Guid>
    {
        private readonly IApplicationDbContext _db;
        public Handler(IApplicationDbContext db) { _db = db; }
        public async Task<Guid> Handle(Command req, CancellationToken ct)
        {
            var code = req.Code.Trim().ToUpperInvariant();
            if (await _db.ServiceTypes.AnyAsync(s => s.Code == code, ct))
                throw new DomainException($"Service code '{code}' already exists.");
            var s = new ServiceType
            {
                Code = code,
                Name = req.Name.Trim(),
                Description = req.Description?.Trim(),
                BasePrice = req.BasePrice,
                EstimatedMinutes = req.EstimatedMinutes,
                IsActive = true
            };
            _db.ServiceTypes.Add(s);
            await _db.SaveChangesAsync(ct);
            return s.Id;
        }
    }
}

public static class UpdateServiceType
{
    public class Command : IRequest<Unit>
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal BasePrice { get; set; }
        public short EstimatedMinutes { get; set; }
        public bool IsActive { get; set; }
    }

    public class Handler : IRequestHandler<Command, Unit>
    {
        private readonly IApplicationDbContext _db;
        public Handler(IApplicationDbContext db) { _db = db; }
        public async Task<Unit> Handle(Command req, CancellationToken ct)
        {
            var s = await _db.ServiceTypes.FirstOrDefaultAsync(x => x.Id == req.Id, ct)
                ?? throw new NotFoundException("ServiceType", req.Id);
            s.Name = req.Name.Trim();
            s.Description = req.Description?.Trim();
            s.BasePrice = req.BasePrice;
            s.EstimatedMinutes = req.EstimatedMinutes;
            s.IsActive = req.IsActive;
            await _db.SaveChangesAsync(ct);
            return Unit.Value;
        }
    }
}

public static class DeleteServiceType
{
    public class Command : IRequest<Unit> { public Guid Id { get; set; } }
    public class Handler : IRequestHandler<Command, Unit>
    {
        private readonly IApplicationDbContext _db;
        public Handler(IApplicationDbContext db) { _db = db; }
        public async Task<Unit> Handle(Command req, CancellationToken ct)
        {
            var s = await _db.ServiceTypes.FirstOrDefaultAsync(x => x.Id == req.Id, ct)
                ?? throw new NotFoundException("ServiceType", req.Id);
            _db.ServiceTypes.Remove(s);
            await _db.SaveChangesAsync(ct);
            return Unit.Value;
        }
    }
}

public static class GetServiceTypes
{
    public class Query : PaginationParams, IRequest<PaginatedList<ServiceTypeDto>>
    {
        public bool? IsActive { get; set; }
    }

    public class Handler : IRequestHandler<Query, PaginatedList<ServiceTypeDto>>
    {
        private readonly IApplicationDbContext _db;
        public Handler(IApplicationDbContext db) { _db = db; }
        public Task<PaginatedList<ServiceTypeDto>> Handle(Query req, CancellationToken ct)
        {
            var q = _db.ServiceTypes.AsNoTracking();
            if (req.IsActive.HasValue) q = q.Where(s => s.IsActive == req.IsActive.Value);
            if (!string.IsNullOrWhiteSpace(req.Search))
            {
                var s = req.Search.Trim().ToLower();
                q = q.Where(x => x.Code.ToLower().Contains(s) || x.Name.ToLower().Contains(s));
            }
            var dtos = q.OrderBy(x => x.Name).Select(x => new ServiceTypeDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                Description = x.Description,
                BasePrice = x.BasePrice,
                EstimatedMinutes = x.EstimatedMinutes,
                IsActive = x.IsActive
            });
            return PaginatedList<ServiceTypeDto>.CreateAsync(dtos, req.Page, req.PageSize, ct);
        }
    }
}
