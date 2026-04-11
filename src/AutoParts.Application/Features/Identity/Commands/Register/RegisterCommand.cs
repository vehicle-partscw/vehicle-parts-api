using AutoParts.Application.Common.Models;
using MediatR;

namespace AutoParts.Application.Features.Identity.Commands.Register;

public class RegisterCommand : IRequest<AuthResult>
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}
