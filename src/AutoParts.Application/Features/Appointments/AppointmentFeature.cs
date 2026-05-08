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

namespace AutoParts.Application.Features.Appointments;

public class AppointmentDto
{
    public Guid Id { get; set; }
    public string CustomerUserId { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public Guid VehicleId { get; set; }
    public string VehicleNumber { get; set; } = string.Empty;
    // surfaced so the new-sale modal can filter the parts dropdown to only those that fit this make
    public string VehicleMake { get; set; } = string.Empty;
    public Guid ServiceTypeId { get; set; }
    public string ServiceTypeName { get; set; } = string.Empty;
    public string? AssignedStaffUserId { get; set; }
    public DateTime ScheduledAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? LinkedInvoiceId { get; set; }
    public string? LinkedInvoiceNumber { get; set; }
}

public static class CreateAppointment
{
    public class Command : IRequest<Guid>
    {
        public Guid VehicleId { get; set; }
        public Guid ServiceTypeId { get; set; }
        public DateTime ScheduledAt { get; set; }
        public string? Notes { get; set; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.VehicleId).NotEmpty();
            RuleFor(x => x.ServiceTypeId).NotEmpty();
            RuleFor(x => x.ScheduledAt).GreaterThan(DateTime.UtcNow.AddMinutes(-5))
                .WithMessage("Appointment must be in the future.");
        }
    }

    public class Handler : IRequestHandler<Command, Guid>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUser _user;
        public Handler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

        public async Task<Guid> Handle(Command req, CancellationToken ct)
        {
            var vehicle = await _db.Vehicles.FirstOrDefaultAsync(v => v.Id == req.VehicleId, ct)
                ?? throw new NotFoundException("Vehicle", req.VehicleId);
            if (!await _db.ServiceTypes.AnyAsync(s => s.Id == req.ServiceTypeId && s.IsActive, ct))
                throw new NotFoundException("ServiceType", req.ServiceTypeId);

            var customerId = _user.IsInRole(Roles.Customer) ? _user.UserId! : vehicle.CustomerUserId;

            var appt = new Appointment
            {
                CustomerUserId = customerId,
                VehicleId = req.VehicleId,
                ServiceTypeId = req.ServiceTypeId,
                ScheduledAt = req.ScheduledAt,
                Status = AppointmentStatus.Pending,
                Notes = req.Notes?.Trim()
            };
            _db.Appointments.Add(appt);
            await _db.SaveChangesAsync(ct);
            return appt.Id;
        }
    }
}

public static class UpdateAppointmentStatus
{
    public class Command : IRequest<Unit>
    {
        public Guid Id { get; set; }
        public AppointmentStatus NewStatus { get; set; }
        public string? AssignedStaffUserId { get; set; }
    }

    public class Handler : IRequestHandler<Command, Unit>
    {
        private readonly IApplicationDbContext _db;
        public Handler(IApplicationDbContext db) { _db = db; }
        public async Task<Unit> Handle(Command req, CancellationToken ct)
        {
            var a = await _db.Appointments.FirstOrDefaultAsync(x => x.Id == req.Id, ct)
                ?? throw new NotFoundException("Appointment", req.Id);

            // simple rule: cannot move from cancelled or done back to pending/confirmed
            if (a.Status is AppointmentStatus.Cancelled or AppointmentStatus.Done
                && req.NewStatus is AppointmentStatus.Pending or AppointmentStatus.Confirmed)
            {
                throw new DomainException("Cannot reopen a finalised appointment.");
            }

            a.Status = req.NewStatus;
            if (!string.IsNullOrWhiteSpace(req.AssignedStaffUserId))
                a.AssignedStaffUserId = req.AssignedStaffUserId;
            await _db.SaveChangesAsync(ct);
            return Unit.Value;
        }
    }
}

public static class GetAppointments
{
    public class Query : PaginationParams, IRequest<PaginatedList<AppointmentDto>>
    {
        public string? CustomerUserId { get; set; }
        public AppointmentStatus? Status { get; set; }
    }

    public class Handler : IRequestHandler<Query, PaginatedList<AppointmentDto>>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUser _user;
        private readonly IIdentityService _identity;
        public Handler(IApplicationDbContext db, ICurrentUser user, IIdentityService identity)
        {
            _db = db;
            _user = user;
            _identity = identity;
        }

        public async Task<PaginatedList<AppointmentDto>> Handle(Query req, CancellationToken ct)
        {
            var q = _db.Appointments
                .Include(a => a.Vehicle)
                .Include(a => a.ServiceType)
                .AsNoTracking();

            if (_user.IsInRole(Roles.Customer))
                q = q.Where(a => a.CustomerUserId == _user.UserId);
            else if (!string.IsNullOrWhiteSpace(req.CustomerUserId))
                q = q.Where(a => a.CustomerUserId == req.CustomerUserId);

            if (req.Status.HasValue) q = q.Where(a => a.Status == req.Status.Value);

            var projected = q.OrderBy(a => a.ScheduledAt).Select(a => new AppointmentDto
            {
                Id = a.Id,
                CustomerUserId = a.CustomerUserId,
                VehicleId = a.VehicleId,
                VehicleNumber = a.Vehicle != null ? a.Vehicle.VehicleNumber : string.Empty,
                VehicleMake = a.Vehicle != null ? a.Vehicle.Make : string.Empty,
                ServiceTypeId = a.ServiceTypeId,
                ServiceTypeName = a.ServiceType != null ? a.ServiceType.Name : string.Empty,
                AssignedStaffUserId = a.AssignedStaffUserId,
                ScheduledAt = a.ScheduledAt,
                Status = a.Status.ToString(),
                Notes = a.Notes,
                CreatedAt = a.CreatedAt,
                // a Done appointment may have an invoice created from it later; expose it on the row
                LinkedInvoiceId = _db.SalesInvoices
                    .Where(s => s.RelatedAppointmentId == a.Id)
                    .Select(s => (Guid?)s.Id)
                    .FirstOrDefault(),
                LinkedInvoiceNumber = _db.SalesInvoices
                    .Where(s => s.RelatedAppointmentId == a.Id)
                    .Select(s => s.InvoiceNumber)
                    .FirstOrDefault()
            });

            var page = await PaginatedList<AppointmentDto>.CreateAsync(projected, req.Page, req.PageSize, ct);

            // staff/admin need to know which customer made the booking; join name + phone post-projection
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
