using AutoParts.Application.Common.Models;
using MediatR;

namespace AutoParts.Application.Features.Vendors.Queries.GetVendorById;

public class GetVendorByIdQuery : IRequest<VendorDto?>
{
    public Guid Id { get; set; }
}
