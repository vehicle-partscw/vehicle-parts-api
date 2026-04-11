using AutoParts.Application.Common.Interfaces;
using MediatR;

namespace AutoParts.Application.Features.Staff.Commands.ToggleStaffActive;

public class ToggleStaffActiveCommandHandler : IRequestHandler<ToggleStaffActiveCommand, bool>
{
    private readonly IIdentityService _identityService;

    public ToggleStaffActiveCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<bool> Handle(ToggleStaffActiveCommand request, CancellationToken cancellationToken)
    {
        return await _identityService.ToggleStaffActiveAsync(request.UserId);
    }
}
