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
    public Guid VehicleId { get; set; }
    public string VehicleNumber { get; set; } = string.Empty;
    public Guid ServiceTypeId { get; set; }
    public string ServiceTypeName { get; set; } = string.Empty;
    public string? AssignedStaffUserId { get; set; }
    public DateTime ScheduledAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
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
        public Handler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

        public Task<PaginatedList<AppointmentDto>> Handle(Query req, CancellationToken ct)
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

            var dtos = q.OrderBy(a => a.ScheduledAt).Select(a => new AppointmentDto
            {
                Id = a.Id,
                CustomerUserId = a.CustomerUserId,
                VehicleId = a.VehicleId,
                VehicleNumber = a.Vehicle != null ? a.Vehicle.VehicleNumber : string.Empty,
                ServiceTypeId = a.ServiceTypeId,
                ServiceTypeName = a.ServiceType != null ? a.ServiceType.Name : string.Empty,
                AssignedStaffUserId = a.AssignedStaffUserId,
                ScheduledAt = a.ScheduledAt,
                Status = a.Status.ToString(),
                Notes = a.Notes,
                CreatedAt = a.CreatedAt
            });
            return PaginatedList<AppointmentDto>.CreateAsync(dtos, req.Page, req.PageSize, ct);
        }
    }
}
