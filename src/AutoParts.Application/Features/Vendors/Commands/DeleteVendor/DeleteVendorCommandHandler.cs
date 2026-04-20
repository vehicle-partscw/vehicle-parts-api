using AutoParts.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AutoParts.Application.Features.Vendors.Commands.DeleteVendor;

public class DeleteVendorCommandHandler : IRequestHandler<DeleteVendorCommand, bool>
{
    private readonly IApplicationDbContext _db;

    public DeleteVendorCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<bool> Handle(DeleteVendorCommand request, CancellationToken cancellationToken)
    {
        var vendor = await _db.Vendors
            .FirstOrDefaultAsync(v => v.Id == request.Id, cancellationToken);

        if (vendor is null) return false;

        // Soft delete via the auditable interceptor (sets IsDeleted on EntityState.Deleted)
        _db.Vendors.Remove(vendor);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
