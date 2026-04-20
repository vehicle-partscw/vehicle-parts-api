using AutoParts.Application.Common.Interfaces;
using AutoParts.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AutoParts.Application.Features.Vendors.Queries.GetVendorById;

public class GetVendorByIdQueryHandler : IRequestHandler<GetVendorByIdQuery, VendorDto?>
{
    private readonly IApplicationDbContext _db;

    public GetVendorByIdQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<VendorDto?> Handle(GetVendorByIdQuery request, CancellationToken cancellationToken)
    {
        return await _db.Vendors
            .Where(v => v.Id == request.Id)
            .Select(v => new VendorDto
            {
                Id = v.Id,
                Name = v.Name,
                ContactPerson = v.ContactPerson,
                Phone = v.Phone,
                Email = v.Email,
                Address = v.Address,
                IsActive = v.IsActive,
                CreatedAt = v.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
