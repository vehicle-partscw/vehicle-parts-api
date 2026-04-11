using AutoParts.Application.Common.Interfaces;
using AutoParts.Application.Common.Models;
using MediatR;

namespace AutoParts.Application.Features.Staff.Commands.CreateStaff;

public class CreateStaffCommandHandler : IRequestHandler<CreateStaffCommand, AuthResult>
{
    private readonly IIdentityService _identityService;

    public CreateStaffCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<AuthResult> Handle(CreateStaffCommand request, CancellationToken cancellationToken)
    {
        return await _identityService.RegisterStaffAsync(request.FullName, request.Email, request.Password, request.Role);
    }
}
