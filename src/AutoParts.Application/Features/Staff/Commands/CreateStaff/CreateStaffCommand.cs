using AutoParts.Application.Common.Models;
using MediatR;

namespace AutoParts.Application.Features.Staff.Commands.CreateStaff;

public class CreateStaffCommand : IRequest<AuthResult>
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
