using AutoParts.Application.Common.Exceptions;
using AutoParts.Application.Common.Interfaces;
using AutoParts.Application.Common.Models;
using AutoParts.Domain.Common;
using AutoParts.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AutoParts.Application.Features.PartCategories;

public class PartCategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}

public static class CreatePartCategory
{
    public class Command : IRequest<Guid>
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(60);
        }
    }

    public class Handler : IRequestHandler<Command, Guid>
    {
        private readonly IApplicationDbContext _db;
        public Handler(IApplicationDbContext db) { _db = db; }

        public async Task<Guid> Handle(Command request, CancellationToken ct)
        {
            var name = request.Name.Trim();

            if (await _db.PartCategories.AnyAsync(c => c.Name == name, ct))
                throw new DomainException($"Category '{name}' already exists.");

            var entity = new PartCategory { Name = name, Description = request.Description?.Trim() };
            _db.PartCategories.Add(entity);
            await _db.SaveChangesAsync(ct);
            return entity.Id;
        }
    }
}

public static class UpdatePartCategory
{
    public class Command : IRequest<Unit>
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Name).NotEmpty().MaximumLength(60);
        }
    }

    public class Handler : IRequestHandler<Command, Unit>
    {
        private readonly IApplicationDbContext _db;
        public Handler(IApplicationDbContext db) { _db = db; }

        public async Task<Unit> Handle(Command request, CancellationToken ct)
        {
            var entity = await _db.PartCategories.FirstOrDefaultAsync(c => c.Id == request.Id, ct)
                ?? throw new NotFoundException("PartCategory", request.Id);
            entity.Name = request.Name.Trim();
            entity.Description = request.Description?.Trim();
            await _db.SaveChangesAsync(ct);
            return Unit.Value;
        }
    }
}

public static class DeletePartCategory
{
    public class Command : IRequest<Unit>
    {
        public Guid Id { get; set; }
    }

    public class Handler : IRequestHandler<Command, Unit>
    {
        private readonly IApplicationDbContext _db;
        public Handler(IApplicationDbContext db) { _db = db; }

        public async Task<Unit> Handle(Command request, CancellationToken ct)
        {
            var entity = await _db.PartCategories.FirstOrDefaultAsync(c => c.Id == request.Id, ct)
                ?? throw new NotFoundException("PartCategory", request.Id);
            _db.PartCategories.Remove(entity);
            await _db.SaveChangesAsync(ct);
            return Unit.Value;
        }
    }
}

public static class GetPartCategoryById
{
    public class Query : IRequest<PartCategoryDto?>
    {
        public Guid Id { get; set; }
    }

    public class Handler : IRequestHandler<Query, PartCategoryDto?>
    {
        private readonly IApplicationDbContext _db;
        public Handler(IApplicationDbContext db) { _db = db; }

        public Task<PartCategoryDto?> Handle(Query request, CancellationToken ct) =>
            _db.PartCategories
                .Where(c => c.Id == request.Id)
                .Select(c => new PartCategoryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    CreatedAt = c.CreatedAt
                })
                .FirstOrDefaultAsync(ct);
    }
}

public static class GetPartCategories
{
    public class Query : PaginationParams, IRequest<PaginatedList<PartCategoryDto>> { }

    public class Handler : IRequestHandler<Query, PaginatedList<PartCategoryDto>>
    {
        private readonly IApplicationDbContext _db;
        public Handler(IApplicationDbContext db) { _db = db; }

        public Task<PaginatedList<PartCategoryDto>> Handle(Query request, CancellationToken ct)
        {
            var q = _db.PartCategories.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var s = request.Search.Trim().ToLower();
                q = q.Where(c => c.Name.ToLower().Contains(s));
            }
            var dtos = q.OrderBy(c => c.Name).Select(c => new PartCategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                CreatedAt = c.CreatedAt
            });
            return PaginatedList<PartCategoryDto>.CreateAsync(dtos, request.Page, request.PageSize, ct);
        }
    }
}
