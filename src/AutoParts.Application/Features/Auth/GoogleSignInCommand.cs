using AutoParts.Application.Common.Models;
using FluentValidation;
using MediatR;

namespace AutoParts.Application.Features.Auth;

public class GoogleSignInCommand : IRequest<AuthResult>
{
    public string? Code { get; set; }
    public string? IdToken { get; set; }
}

public class GoogleSignInCommandValidator : AbstractValidator<GoogleSignInCommand>
{
    public GoogleSignInCommandValidator()
    {
        RuleFor(x => x).Must(x => !string.IsNullOrWhiteSpace(x.Code) || !string.IsNullOrWhiteSpace(x.IdToken))
            .WithMessage("Either Code or IdToken is required.");
    }
}
