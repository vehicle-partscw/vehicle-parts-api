using MediatR;

namespace AutoParts.Application.Features.Vendors.Commands.UpdateVendor;

public class UpdateVendorCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ContactPerson { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Address { get; set; }
    public bool IsActive { get; set; }
}
