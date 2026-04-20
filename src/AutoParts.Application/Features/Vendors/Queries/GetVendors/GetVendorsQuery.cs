using AutoParts.Application.Common.Models;
using MediatR;

namespace AutoParts.Application.Features.Vendors.Queries.GetVendors;

public class GetVendorsQuery : PaginationParams, IRequest<PaginatedList<VendorDto>>
{
    public bool? IsActive { get; set; }
}
