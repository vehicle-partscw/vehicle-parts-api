using MediatR;

namespace AutoParts.Application.Features.Vendors.Commands.DeleteVendor;

public class DeleteVendorCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}
