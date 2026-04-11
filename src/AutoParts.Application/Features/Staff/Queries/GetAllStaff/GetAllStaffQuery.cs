using AutoParts.Application.Common.Models;
using MediatR;

namespace AutoParts.Application.Features.Staff.Queries.GetAllStaff;

public class GetAllStaffQuery : IRequest<IReadOnlyList<StaffDto>>
{
}
