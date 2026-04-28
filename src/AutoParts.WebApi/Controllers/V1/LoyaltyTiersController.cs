using AutoParts.Application.Common.Security;
using AutoParts.Application.Features.LoyaltyTiers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoParts.WebApi.Controllers.V1;

[ApiController]
[Route("api/v1/loyalty-tiers")]
[Authorize(Roles = Roles.Admin)]
public class LoyaltyTiersController : ControllerBase
{
    private readonly IMediator _mediator;
    public LoyaltyTiersController(IMediator mediator) { _mediator = mediator; }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLoyaltyTier.Command command)
    {
        var id = await _mediator.Send(command);
        return Created($"/api/v1/loyalty-tiers/{id}", new { id });
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] GetLoyaltyTiers.Query query) =>
        Ok(await _mediator.Send(query));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateLoyaltyTier.Command command)
    {
        command.Id = id;
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _mediator.Send(new DeleteLoyaltyTier.Command { Id = id });
        return NoContent();
    }
}
