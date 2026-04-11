using MediatR;

namespace AutoParts.Application.Features.Staff.Commands.UpdateStaffRole;

public class UpdateStaffRoleCommand : IRequest<bool>
{
    public string UserId { get; set; } = string.Empty;
    public string NewRole { get; set; } = string.Empty;
}
