using AutoParts.Application.Common.Models;
using MediatR;

namespace AutoParts.Application.Features.Identity.Commands.Login;

public class LoginCommand : IRequest<AuthResult>
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
