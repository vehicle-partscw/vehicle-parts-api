using AutoParts.Application.Common.Exceptions;
using AutoParts.Application.Common.Interfaces;
using AutoParts.Application.Common.Models;
using AutoParts.Application.Common.Security;
using AutoParts.Domain.Common;
using AutoParts.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AutoParts.Application.Features.Vehicles;

public class VehicleDto
{
    public Guid Id { get; set; }
    public string CustomerUserId { get; set; } = string.Empty;
    public string VehicleNumber { get; set; } = string.Empty;
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public short Year { get; set; }
    public string? Vin { get; set; }
    public int Mileage { get; set; }
    public DateTime CreatedAt { get; set; }
}

public static class CreateVehicle
{
    public class Command : IRequest<Guid>
    {
        public string CustomerUserId { get; set; } = string.Empty;
        public string VehicleNumber { get; set; } = string.Empty;
        public string Make { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public short Year { get; set; }
        public string? Vin { get; set; }
        public int Mileage { get; set; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.CustomerUserId).NotEmpty();
            RuleFor(x => x.VehicleNumber).NotEmpty().MaximumLength(20);
            RuleFor(x => x.Make).NotEmpty().MaximumLength(40);
            RuleFor(x => x.Model).NotEmpty().MaximumLength(60);
            RuleFor(x => x.Year).InclusiveBetween((short)1980, (short)2100);
            RuleFor(x => x.Vin).MaximumLength(17);
            RuleFor(x => x.Mileage).GreaterThanOrEqualTo(0);
        }
    }

    public class Handler : IRequestHandler<Command, Guid>
    {
        private readonly IApplicationDbContext _db;
        public Handler(IApplicationDbContext db) { _db = db; }
        public async Task<Guid> Handle(Command request, CancellationToken ct)
        {
            var num = request.VehicleNumber.Trim().ToUpperInvariant();
            if (await _db.Vehicles.AnyAsync(v => v.VehicleNumber == num, ct))
                throw new DomainException($"Vehicle number '{num}' is already registered.");

            var vehicle = new Vehicle
            {
                CustomerUserId = request.CustomerUserId,
                VehicleNumber = num,
                Make = request.Make.Trim(),
                Model = request.Model.Trim(),
                Year = request.Year,
                Vin = string.IsNullOrWhiteSpace(request.Vin) ? null : request.Vin.Trim().ToUpperInvariant(),
                Mileage = request.Mileage
            };
            _db.Vehicles.Add(vehicle);
            await _db.SaveChangesAsync(ct);
            return vehicle.Id;
        }
    }
}

public static class UpdateVehicle
{
    public class Command : IRequest<Unit>
    {
        public Guid Id { get; set; }
        public string Make { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public short Year { get; set; }
        public string? Vin { get; set; }
        public int Mileage { get; set; }
    }

    public class Handler : IRequestHandler<Command, Unit>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUser _user;
        public Handler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

        public async Task<Unit> Handle(Command req, CancellationToken ct)
        {
            var vehicle = await _db.Vehicles.FirstOrDefaultAsync(v => v.Id == req.Id, ct)
                ?? throw new NotFoundException("Vehicle", req.Id);

            if (_user.IsInRole(Roles.Customer) && vehicle.CustomerUserId != _user.UserId)
                throw new ForbiddenException();

            vehicle.Make = req.Make.Trim();
            vehicle.Model = req.Model.Trim();
            vehicle.Year = req.Year;
            vehicle.Vin = string.IsNullOrWhiteSpace(req.Vin) ? null : req.Vin.Trim().ToUpperInvariant();
            vehicle.Mileage = req.Mileage;
            await _db.SaveChangesAsync(ct);
            return Unit.Value;
        }
    }
}

public static class DeleteVehicle
{
    public class Command : IRequest<Unit> { public Guid Id { get; set; } }

    public class Handler : IRequestHandler<Command, Unit>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUser _user;
        public Handler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

        public async Task<Unit> Handle(Command req, CancellationToken ct)
        {
            var vehicle = await _db.Vehicles.FirstOrDefaultAsync(v => v.Id == req.Id, ct)
                ?? throw new NotFoundException("Vehicle", req.Id);
            if (_user.IsInRole(Roles.Customer) && vehicle.CustomerUserId != _user.UserId)
                throw new ForbiddenException();
            _db.Vehicles.Remove(vehicle);
            await _db.SaveChangesAsync(ct);
            return Unit.Value;
        }
    }
}

public static class GetVehicleById
{
    public class Query : IRequest<VehicleDto?> { public Guid Id { get; set; } }

    public class Handler : IRequestHandler<Query, VehicleDto?>
    {
        private readonly IApplicationDbContext _db;
        public Handler(IApplicationDbContext db) { _db = db; }
        public Task<VehicleDto?> Handle(Query req, CancellationToken ct) =>
            _db.Vehicles
                .Where(v => v.Id == req.Id)
                .Select(v => new VehicleDto
                {
                    Id = v.Id,
                    CustomerUserId = v.CustomerUserId,
                    VehicleNumber = v.VehicleNumber,
                    Make = v.Make,
                    Model = v.Model,
                    Year = v.Year,
                    Vin = v.Vin,
                    Mileage = v.Mileage,
                    CreatedAt = v.CreatedAt
                })
                .FirstOrDefaultAsync(ct);
    }
}

public static class GetVehicles
{
    public class Query : PaginationParams, IRequest<PaginatedList<VehicleDto>>
    {
        public string? CustomerUserId { get; set; }
    }

    public class Handler : IRequestHandler<Query, PaginatedList<VehicleDto>>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUser _user;
        public Handler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

        public Task<PaginatedList<VehicleDto>> Handle(Query req, CancellationToken ct)
        {
            var q = _db.Vehicles.AsNoTracking();

            if (_user.IsInRole(Roles.Customer))
            {
                q = q.Where(v => v.CustomerUserId == _user.UserId);
            }
            else if (!string.IsNullOrWhiteSpace(req.CustomerUserId))
            {
                q = q.Where(v => v.CustomerUserId == req.CustomerUserId);
            }

            if (!string.IsNullOrWhiteSpace(req.Search))
            {
                var s = req.Search.Trim().ToLower();
                q = q.Where(v =>
                    v.VehicleNumber.ToLower().Contains(s) ||
                    v.Make.ToLower().Contains(s) ||
                    v.Model.ToLower().Contains(s));
            }

            var dtos = q.OrderByDescending(v => v.CreatedAt).Select(v => new VehicleDto
            {
                Id = v.Id,
                CustomerUserId = v.CustomerUserId,
                VehicleNumber = v.VehicleNumber,
                Make = v.Make,
                Model = v.Model,
                Year = v.Year,
                Vin = v.Vin,
                Mileage = v.Mileage,
                CreatedAt = v.CreatedAt
            });
            return PaginatedList<VehicleDto>.CreateAsync(dtos, req.Page, req.PageSize, ct);
        }
    }
}
