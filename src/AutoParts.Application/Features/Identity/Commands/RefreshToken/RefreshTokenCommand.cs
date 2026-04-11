using AutoParts.Application.Common.Models;
using MediatR;

namespace AutoParts.Application.Features.Identity.Commands.RefreshToken;

public class RefreshTokenCommand : IRequest<AuthResult>
{
    public string RefreshToken { get; set; } = string.Empty;
}
