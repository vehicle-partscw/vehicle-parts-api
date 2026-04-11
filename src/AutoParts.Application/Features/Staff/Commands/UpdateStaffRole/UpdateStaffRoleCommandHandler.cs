using AutoParts.Application.Common.Interfaces;
using MediatR;

namespace AutoParts.Application.Features.Staff.Commands.UpdateStaffRole;

public class UpdateStaffRoleCommandHandler : IRequestHandler<UpdateStaffRoleCommand, bool>
{
    private readonly IIdentityService _identityService;

    public UpdateStaffRoleCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<bool> Handle(UpdateStaffRoleCommand request, CancellationToken cancellationToken)
    {
        return await _identityService.UpdateStaffRoleAsync(request.UserId, request.NewRole);
    }
}
