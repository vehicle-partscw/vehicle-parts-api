using AutoParts.Application.Common.Exceptions;
using AutoParts.Application.Common.Interfaces;
using AutoParts.Application.Common.Models;
using AutoParts.Application.Common.Security;
using AutoParts.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AutoParts.Application.Features.Engagement;

public class PartRequestDto
{
    public Guid Id { get; set; }
    public string CustomerUserId { get; set; } = string.Empty;
    public string PartName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}

public static class CreatePartRequest
{
    public class Command : IRequest<Guid>
    {
        public string PartName { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.PartName).NotEmpty().MaximumLength(120);
        }
    }

    public class Handler : IRequestHandler<Command, Guid>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUser _user;
        public Handler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

        public async Task<Guid> Handle(Command req, CancellationToken ct)
        {
            var pr = new PartRequest
            {
                CustomerUserId = _user.UserId ?? string.Empty,
                PartName = req.PartName.Trim(),
                Description = req.Description?.Trim(),
                Status = PartRequestStatus.Pending,
                RequestedAt = DateTime.UtcNow
            };
            _db.PartRequests.Add(pr);
            await _db.SaveChangesAsync(ct);
            return pr.Id;
        }
    }
}

public static class UpdatePartRequestStatus
{
    public class Command : IRequest<Unit>
    {
        public Guid Id { get; set; }
        public PartRequestStatus NewStatus { get; set; }
    }

    public class Handler : IRequestHandler<Command, Unit>
    {
        private readonly IApplicationDbContext _db;
        public Handler(IApplicationDbContext db) { _db = db; }
        public async Task<Unit> Handle(Command req, CancellationToken ct)
        {
            var pr = await _db.PartRequests.FirstOrDefaultAsync(x => x.Id == req.Id, ct)
                ?? throw new NotFoundException("PartRequest", req.Id);
            pr.Status = req.NewStatus;
            if (req.NewStatus is PartRequestStatus.Sourced or PartRequestStatus.Rejected)
                pr.ResolvedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return Unit.Value;
        }
    }
}

public static class GetPartRequests
{
    public class Query : PaginationParams, IRequest<PaginatedList<PartRequestDto>>
    {
        public PartRequestStatus? Status { get; set; }
    }

    public class Handler : IRequestHandler<Query, PaginatedList<PartRequestDto>>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUser _user;
        public Handler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }
        public Task<PaginatedList<PartRequestDto>> Handle(Query req, CancellationToken ct)
        {
            var q = _db.PartRequests.AsNoTracking();
            if (_user.IsInRole(Roles.Customer))
                q = q.Where(p => p.CustomerUserId == _user.UserId);
            if (req.Status.HasValue) q = q.Where(p => p.Status == req.Status.Value);

            var dtos = q.OrderByDescending(p => p.RequestedAt).Select(p => new PartRequestDto
            {
                Id = p.Id,
                CustomerUserId = p.CustomerUserId,
                PartName = p.PartName,
                Description = p.Description,
                Status = p.Status.ToString(),
                RequestedAt = p.RequestedAt,
                ResolvedAt = p.ResolvedAt
            });
            return PaginatedList<PartRequestDto>.CreateAsync(dtos, req.Page, req.PageSize, ct);
        }
    }
}

public class ReviewDto
{
    public Guid Id { get; set; }
    public string CustomerUserId { get; set; } = string.Empty;
    public short Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}

public static class CreateReview
{
    public class Command : IRequest<Guid>
    {
        public short Rating { get; set; }
        public string? Comment { get; set; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Rating).InclusiveBetween((short)1, (short)5);
            RuleFor(x => x.Comment).MaximumLength(1000);
        }
    }

    public class Handler : IRequestHandler<Command, Guid>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUser _user;
        public Handler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }
        public async Task<Guid> Handle(Command req, CancellationToken ct)
        {
            var r = new Review
            {
                CustomerUserId = _user.UserId ?? string.Empty,
                Rating = req.Rating,
                Comment = req.Comment?.Trim()
            };
            _db.Reviews.Add(r);
            await _db.SaveChangesAsync(ct);
            return r.Id;
        }
    }
}

public static class GetReviews
{
    public class Query : PaginationParams, IRequest<PaginatedList<ReviewDto>> { }
    public class Handler : IRequestHandler<Query, PaginatedList<ReviewDto>>
    {
        private readonly IApplicationDbContext _db;
        public Handler(IApplicationDbContext db) { _db = db; }
        public Task<PaginatedList<ReviewDto>> Handle(Query req, CancellationToken ct)
        {
            var q = _db.Reviews.AsNoTracking()
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new ReviewDto
                {
                    Id = r.Id,
                    CustomerUserId = r.CustomerUserId,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt
                });
            return PaginatedList<ReviewDto>.CreateAsync(q, req.Page, req.PageSize, ct);
        }
    }
}

public class NotificationDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? RelatedEntityName { get; set; }
    public string? RelatedEntityId { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public static class GetMyNotifications
{
    public class Query : PaginationParams, IRequest<PaginatedList<NotificationDto>>
    {
        public bool? Unread { get; set; }
    }

    public class Handler : IRequestHandler<Query, PaginatedList<NotificationDto>>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUser _user;
        public Handler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }
        public Task<PaginatedList<NotificationDto>> Handle(Query req, CancellationToken ct)
        {
            var q = _db.Notifications.AsNoTracking()
                .Where(n => n.RecipientUserId == _user.UserId);
            if (req.Unread == true) q = q.Where(n => n.ReadAt == null);

            var dtos = q.OrderByDescending(n => n.CreatedAt).Select(n => new NotificationDto
            {
                Id = n.Id,
                Type = n.Type.ToString(),
                Title = n.Title,
                Body = n.Body,
                RelatedEntityName = n.RelatedEntityName,
                RelatedEntityId = n.RelatedEntityId,
                ReadAt = n.ReadAt,
                CreatedAt = n.CreatedAt
            });
            return PaginatedList<NotificationDto>.CreateAsync(dtos, req.Page, req.PageSize, ct);
        }
    }
}

public static class MarkNotificationRead
{
    public class Command : IRequest<Unit> { public Guid Id { get; set; } }
    public class Handler : IRequestHandler<Command, Unit>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUser _user;
        public Handler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }
        public async Task<Unit> Handle(Command req, CancellationToken ct)
        {
            var n = await _db.Notifications.FirstOrDefaultAsync(x => x.Id == req.Id, ct)
                ?? throw new NotFoundException("Notification", req.Id);
            if (n.RecipientUserId != _user.UserId)
                throw new ForbiddenException();
            n.ReadAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return Unit.Value;
        }
    }
}

public class AiPredictionDto
{
    public Guid Id { get; set; }
    public Guid VehicleId { get; set; }
    public Guid PartId { get; set; }
    public decimal FailureProbability { get; set; }
    public short WindowDays { get; set; }
    public string ModelVersion { get; set; } = string.Empty;
    public DateTime PredictedAt { get; set; }
}

public static class GetMyAiPredictions
{
    public class Query : PaginationParams, IRequest<PaginatedList<AiPredictionDto>> { }

    public class Handler : IRequestHandler<Query, PaginatedList<AiPredictionDto>>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUser _user;
        public Handler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }
        public Task<PaginatedList<AiPredictionDto>> Handle(Query req, CancellationToken ct)
        {
            var myVehicleIds = _db.Vehicles
                .Where(v => v.CustomerUserId == _user.UserId)
                .Select(v => v.Id);

            var q = _db.AiPredictions.AsNoTracking()
                .Where(p => myVehicleIds.Contains(p.VehicleId))
                .OrderByDescending(p => p.PredictedAt)
                .Select(p => new AiPredictionDto
                {
                    Id = p.Id,
                    VehicleId = p.VehicleId,
                    PartId = p.PartId,
                    FailureProbability = p.FailureProbability,
                    WindowDays = p.WindowDays,
                    ModelVersion = p.ModelVersion,
                    PredictedAt = p.PredictedAt
                });
            return PaginatedList<AiPredictionDto>.CreateAsync(q, req.Page, req.PageSize, ct);
        }
    }
}
