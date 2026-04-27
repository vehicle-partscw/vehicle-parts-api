using AutoParts.Application.Features.Me;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoParts.WebApi.Controllers.V1;

[ApiController]
[Route("api/v1/me")]
[Authorize]
public class MeController : ControllerBase
{
    private readonly IMediator _mediator;
    public MeController(IMediator mediator) { _mediator = mediator; }

    [HttpGet]
    public async Task<IActionResult> GetProfile() =>
        Ok(await _mediator.Send(new GetMyProfile.Query()));

    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateMyProfile.Command command)
    {
        await _mediator.Send(command);
        return NoContent();
    }
    [HttpPut("email")]
    public async Task<IActionResult> ChangeEmail([FromBody] ChangeMyEmail.Command command)
    {
        await _mediator.Send(command);
        return NoContent();
    }
    [HttpPut("password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangeMyPassword.Command command)
    {
        await _mediator.Send(command);
        return NoContent();
    }
}
