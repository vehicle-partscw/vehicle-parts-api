using AutoParts.Application.Common.Interfaces;
using AutoParts.Application.Common.Models;
using MediatR;

namespace AutoParts.Application.Features.Staff.Queries.GetAllStaff;

public class GetAllStaffQueryHandler : IRequestHandler<GetAllStaffQuery, IReadOnlyList<StaffDto>>
{
    private readonly IIdentityService _identityService;

    public GetAllStaffQueryHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<IReadOnlyList<StaffDto>> Handle(GetAllStaffQuery request, CancellationToken cancellationToken)
    {
        return await _identityService.GetAllStaffAsync();
    }
}
