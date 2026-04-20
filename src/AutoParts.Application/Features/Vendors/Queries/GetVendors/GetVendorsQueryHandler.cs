using AutoParts.Application.Common.Interfaces;
using AutoParts.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AutoParts.Application.Features.Vendors.Queries.GetVendors;

public class GetVendorsQueryHandler : IRequestHandler<GetVendorsQuery, PaginatedList<VendorDto>>
{
    private readonly IApplicationDbContext _db;

    public GetVendorsQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public Task<PaginatedList<VendorDto>> Handle(GetVendorsQuery request, CancellationToken cancellationToken)
    {
        var q = _db.Vendors.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim().ToLower();
            q = q.Where(v =>
                v.Name.ToLower().Contains(s) ||
                v.ContactPerson.ToLower().Contains(s) ||
                v.Email.ToLower().Contains(s));
        }

        if (request.IsActive.HasValue)
        {
            q = q.Where(v => v.IsActive == request.IsActive.Value);
        }

        q = q.OrderBy(v => v.Name);

        var projected = q.Select(v => new VendorDto
        {
            Id = v.Id,
            Name = v.Name,
            ContactPerson = v.ContactPerson,
            Phone = v.Phone,
            Email = v.Email,
            Address = v.Address,
            IsActive = v.IsActive,
            CreatedAt = v.CreatedAt
        });

        return PaginatedList<VendorDto>.CreateAsync(projected, request.Page, request.PageSize, cancellationToken);
    }
}
