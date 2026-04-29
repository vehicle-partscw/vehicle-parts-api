using AutoParts.Application.Common.Exceptions;
using AutoParts.Application.Common.Interfaces;
using AutoParts.Application.Common.Models;
using AutoParts.Application.Common.Security;
using AutoParts.Domain.Common;
using AutoParts.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AutoParts.Application.Features.Engagement;

public class PartRequestDto
{
    public Guid Id { get; set; }
    public string CustomerUserId { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public string PartName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public Guid? ResolvedPartId { get; set; }
    public string? ResolvedPartSku { get; set; }
    public string? ResolvedPartName { get; set; }
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

// staff sources a part request by adding it to the catalog. Creates a Part,
// links it to the request, marks the request Sourced, and notifies the customer.
public static class SourcePartRequest
{
    public class Command : IRequest<Unit>
    {
        public Guid Id { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid CategoryId { get; set; }
        public Guid VendorId { get; set; }
        public decimal UnitPrice { get; set; }
        public int InitialStock { get; set; }
        public short ReorderLevel { get; set; } = 10;
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Sku).NotEmpty().MaximumLength(40)
                .Matches("^[A-Z0-9-]+$").WithMessage("SKU must be uppercase letters, digits or hyphens.");
            RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
            RuleFor(x => x.CategoryId).NotEmpty();
            RuleFor(x => x.VendorId).NotEmpty();
            RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
            RuleFor(x => x.InitialStock).GreaterThanOrEqualTo(0);
            RuleFor(x => x.ReorderLevel).GreaterThanOrEqualTo((short)0);
        }
    }

    public class Handler : IRequestHandler<Command, Unit>
    {
        private readonly IApplicationDbContext _db;
        public Handler(IApplicationDbContext db) { _db = db; }

        public async Task<Unit> Handle(Command req, CancellationToken ct)
        {
            var pr = await _db.PartRequests.FirstOrDefaultAsync(x => x.Id == req.Id, ct)
                ?? throw new NotFoundException("PartRequest", req.Id);

            if (pr.Status != PartRequestStatus.Pending)
                throw new DomainException("Only pending requests can be sourced.");

            var sku = req.Sku.Trim().ToUpperInvariant();
            if (await _db.Parts.IgnoreQueryFilters().AnyAsync(p => p.Sku == sku, ct))
                throw new DomainException($"SKU '{sku}' is already in use.");

            if (!await _db.PartCategories.AnyAsync(c => c.Id == req.CategoryId, ct))
                throw new NotFoundException("PartCategory", req.CategoryId);
            if (!await _db.Vendors.AnyAsync(v => v.Id == req.VendorId, ct))
                throw new NotFoundException("Vendor", req.VendorId);

            // 1. create the catalog entry
            var part = new Part
            {
                Sku = sku,
                Name = req.Name.Trim(),
                Description = req.Description?.Trim(),
                CategoryId = req.CategoryId,
                VendorId = req.VendorId,
                UnitPrice = req.UnitPrice,
                StockQty = req.InitialStock,
                ReorderLevel = req.ReorderLevel
            };
            _db.Parts.Add(part);

            // 2. resolve the request and link to the new part
            pr.Status = PartRequestStatus.Sourced;
            pr.ResolvedAt = DateTime.UtcNow;
            pr.ResolvedPart = part; // EF will set ResolvedPartId once part.Id is generated

            // 3. notify the customer
            _db.Notifications.Add(new Notification
            {
                RecipientUserId = pr.CustomerUserId,
                Type = NotificationType.PartRequest,
                Title = "Your requested part is now available",
                Body = $"{req.Name.Trim()} is now in our catalogue. Visit Inventory to view it or contact us to arrange a fitting.",
                RelatedEntityName = "Part",
                RelatedEntityId = null // set below after save (Id is generated then)
            });

            await _db.SaveChangesAsync(ct);

            // backfill the notification's RelatedEntityId with the new Part.Id
            var notif = _db.Notifications
                .OrderByDescending(n => n.CreatedAt)
                .First(n => n.RecipientUserId == pr.CustomerUserId && n.RelatedEntityName == "Part" && n.RelatedEntityId == null);
            notif.RelatedEntityId = part.Id.ToString();
            await _db.SaveChangesAsync(ct);

            return Unit.Value;
        }
    }
}

// staff rejects a request (e.g. cannot be sourced). Notifies the customer.
public static class RejectPartRequest
{
    public class Command : IRequest<Unit>
    {
        public Guid Id { get; set; }
        public string? Reason { get; set; }
    }

    public class Handler : IRequestHandler<Command, Unit>
    {
        private readonly IApplicationDbContext _db;
        public Handler(IApplicationDbContext db) { _db = db; }
        public async Task<Unit> Handle(Command req, CancellationToken ct)
        {
            var pr = await _db.PartRequests.FirstOrDefaultAsync(x => x.Id == req.Id, ct)
                ?? throw new NotFoundException("PartRequest", req.Id);
            if (pr.Status != PartRequestStatus.Pending)
                throw new DomainException("Only pending requests can be rejected.");

            pr.Status = PartRequestStatus.Rejected;
            pr.ResolvedAt = DateTime.UtcNow;

            _db.Notifications.Add(new Notification
            {
                RecipientUserId = pr.CustomerUserId,
                Type = NotificationType.PartRequest,
                Title = "Part request update",
                Body = string.IsNullOrWhiteSpace(req.Reason)
                    ? $"We were unable to source {pr.PartName} at this time."
                    : $"We were unable to source {pr.PartName}. Reason: {req.Reason.Trim()}",
                RelatedEntityName = "PartRequest",
                RelatedEntityId = pr.Id.ToString()
            });

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
        private readonly IIdentityService _identity;
        public Handler(IApplicationDbContext db, ICurrentUser user, IIdentityService identity)
        {
            _db = db; _user = user; _identity = identity;
        }
        public async Task<PaginatedList<PartRequestDto>> Handle(Query req, CancellationToken ct)
        {
            var q = _db.PartRequests.AsNoTracking();
            if (_user.IsInRole(Roles.Customer))
                q = q.Where(p => p.CustomerUserId == _user.UserId);
            if (req.Status.HasValue) q = q.Where(p => p.Status == req.Status.Value);

            var projected = q.OrderByDescending(p => p.RequestedAt).Select(p => new PartRequestDto
            {
                Id = p.Id,
                CustomerUserId = p.CustomerUserId,
                PartName = p.PartName,
                Description = p.Description,
                Status = p.Status.ToString(),
                RequestedAt = p.RequestedAt,
                ResolvedAt = p.ResolvedAt,
                ResolvedPartId = p.ResolvedPartId,
                ResolvedPartSku = p.ResolvedPart != null ? p.ResolvedPart.Sku : null,
                ResolvedPartName = p.ResolvedPart != null ? p.ResolvedPart.Name : null
            });

            var page = await PaginatedList<PartRequestDto>.CreateAsync(projected, req.Page, req.PageSize, ct);

            if (!_user.IsInRole(Roles.Customer) && page.Items.Count > 0)
            {
                var summaries = await _identity.GetUserSummariesAsync(page.Items.Select(d => d.CustomerUserId));
                foreach (var d in page.Items)
                {
                    if (summaries.TryGetValue(d.CustomerUserId, out var s))
                    {
                        d.CustomerName = s.FullName;
                        d.CustomerPhone = s.Phone;
                    }
                }
            }

            return page;
        }
    }
}

public class ReviewDto
{
    public Guid Id { get; set; }
    public string CustomerUserId { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
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
        private readonly ICurrentUser _user;
        private readonly IIdentityService _identity;
        public Handler(IApplicationDbContext db, ICurrentUser user, IIdentityService identity)
        {
            _db = db; _user = user; _identity = identity;
        }
        public async Task<PaginatedList<ReviewDto>> Handle(Query req, CancellationToken ct)
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

            var page = await PaginatedList<ReviewDto>.CreateAsync(q, req.Page, req.PageSize, ct);

            // attach reviewer name so staff/admin see who left it
            if (!_user.IsInRole(Roles.Customer) && page.Items.Count > 0)
            {
                var summaries = await _identity.GetUserSummariesAsync(page.Items.Select(d => d.CustomerUserId));
                foreach (var d in page.Items)
                {
                    if (summaries.TryGetValue(d.CustomerUserId, out var s))
                        d.CustomerName = s.FullName;
                }
            }

            return page;
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

public static class GetMyUnreadCount
{
    public class Query : IRequest<int> { }
    public class Handler : IRequestHandler<Query, int>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUser _user;
        public Handler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }
        public Task<int> Handle(Query req, CancellationToken ct) =>
            _db.Notifications
                .Where(n => n.RecipientUserId == _user.UserId && n.ReadAt == null)
                .CountAsync(ct);
    }
}

public static class MarkAllNotificationsRead
{
    public class Command : IRequest<int> { }
    public class Handler : IRequestHandler<Command, int>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUser _user;
        public Handler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }
        public async Task<int> Handle(Command req, CancellationToken ct)
        {
            var unread = await _db.Notifications
                .Where(n => n.RecipientUserId == _user.UserId && n.ReadAt == null)
                .ToListAsync(ct);
            var now = DateTime.UtcNow;
            foreach (var n in unread) n.ReadAt = now;
            await _db.SaveChangesAsync(ct);
            return unread.Count;
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
