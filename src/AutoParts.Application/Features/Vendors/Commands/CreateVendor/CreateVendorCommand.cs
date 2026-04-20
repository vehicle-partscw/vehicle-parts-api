using MediatR;

namespace AutoParts.Application.Features.Vendors.Commands.CreateVendor;

public class CreateVendorCommand : IRequest<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string ContactPerson { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Address { get; set; }
}
