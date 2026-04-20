using AutoParts.Application.Common.Interfaces;
using AutoParts.Domain.Entities;
using MediatR;

namespace AutoParts.Application.Features.Vendors.Commands.CreateVendor;

public class CreateVendorCommandHandler : IRequestHandler<CreateVendorCommand, Guid>
{
    private readonly IApplicationDbContext _db;

    public CreateVendorCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Guid> Handle(CreateVendorCommand request, CancellationToken cancellationToken)
    {
        var vendor = new Vendor
        {
            Name = request.Name.Trim(),
            ContactPerson = request.ContactPerson.Trim(),
            Phone = request.Phone.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim(),
            IsActive = true
        };

        _db.Vendors.Add(vendor);
        await _db.SaveChangesAsync(cancellationToken);
        return vendor.Id;
    }
}
