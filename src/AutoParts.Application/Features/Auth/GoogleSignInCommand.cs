using AutoParts.Application.Common.Models;
using FluentValidation;
using MediatR;

namespace AutoParts.Application.Features.Auth;

/// <summary>
/// Sign in (or auto-register) using a Google ID token from the frontend.
/// The frontend obtains the token via Google Identity Services / @react-oauth/google.
/// The backend verifies it, then delegates to IIdentityService to find or create a Customer.
/// </summary>
public class GoogleSignInCommand : IRequest<AuthResult>
{
    /// <summary>Authorization code from Google (auth-code flow).</summary>
    public string? Code { get; set; }
    /// <summary>Or, an ID token directly (implicit / GoogleLogin component flow).</summary>
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
