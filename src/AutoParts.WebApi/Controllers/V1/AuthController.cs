using AutoParts.Application.Features.Auth;
using AutoParts.Application.Features.Identity.Commands.Login;
using AutoParts.Application.Features.Identity.Commands.Register;
using AutoParts.Application.Features.Identity.Commands.RefreshToken;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoParts.WebApi.Controllers.V1;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Customer self-registration.
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterCommand command)
    {
        var result = await _mediator.Send(command);

        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors });
        }

        return Created(string.Empty, new
        {
            result.UserId,
            result.AccessToken,
            result.RefreshToken,
            result.ExpiresAt
        });
    }

    /// <summary>
    /// Login with email and password.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginCommand command)
    {
        var result = await _mediator.Send(command);

        if (!result.Succeeded)
        {
            return Unauthorized(new { errors = result.Errors });
        }

        return Ok(new
        {
            result.AccessToken,
            result.RefreshToken,
            result.ExpiresAt
        });
    }

    /// <summary>
    /// Refresh an expired access token.
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenCommand command)
    {
        var result = await _mediator.Send(command);

        if (!result.Succeeded)
        {
            return Unauthorized(new { errors = result.Errors });
        }

        return Ok(new
        {
            result.AccessToken,
            result.RefreshToken,
            result.ExpiresAt
        });
    }

    /// <summary>Sign in (or auto-register) using a Google ID token from the frontend.</summary>
    [HttpPost("google")]
    [AllowAnonymous]
    public async Task<IActionResult> Google([FromBody] GoogleSignInCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result.Succeeded)
            return Unauthorized(new { errors = result.Errors });
        return Ok(new
        {
            result.AccessToken,
            result.RefreshToken,
            result.ExpiresAt
        });
    }

    /// <summary>Send a 6-digit reset code to the user's email. Always returns 204 - never reveals whether the email exists.</summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] AutoParts.Application.Features.Auth.ForgotPassword.Command command)
    {
        await _mediator.Send(command);
        return NoContent();
    }

    /// <summary>Verify the reset code. On success returns a short-lived reset token used in the next call.</summary>
    [HttpPost("verify-reset-code")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyResetCode([FromBody] AutoParts.Application.Features.Auth.VerifyResetCode.Command command)
    {
        var resetToken = await _mediator.Send(command);
        return Ok(new { resetToken });
    }

    /// <summary>Set a new password using the reset token from the previous step.</summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] AutoParts.Application.Features.Auth.ResetPasswordWithCode.Command command)
    {
        await _mediator.Send(command);
        return NoContent();
    }
}
