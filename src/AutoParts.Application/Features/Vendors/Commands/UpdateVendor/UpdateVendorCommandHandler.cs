using AutoParts.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AutoParts.Application.Features.Vendors.Commands.UpdateVendor;

public class UpdateVendorCommandHandler : IRequestHandler<UpdateVendorCommand, bool>
{
    private readonly IApplicationDbContext _db;

    public UpdateVendorCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<bool> Handle(UpdateVendorCommand request, CancellationToken cancellationToken)
    {
        var vendor = await _db.Vendors
            .FirstOrDefaultAsync(v => v.Id == request.Id, cancellationToken);

        if (vendor is null) return false;

        vendor.Name = request.Name.Trim();
        vendor.ContactPerson = request.ContactPerson.Trim();
        vendor.Phone = request.Phone.Trim();
        vendor.Email = request.Email.Trim().ToLowerInvariant();
        vendor.Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim();
        vendor.IsActive = request.IsActive;

        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
