using MediatR;

namespace AutoParts.Application.Features.Staff.Commands.ToggleStaffActive;

public class ToggleStaffActiveCommand : IRequest<bool>
{
    public string UserId { get; set; } = string.Empty;
}
