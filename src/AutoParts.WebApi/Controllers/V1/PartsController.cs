using AutoParts.Application.Common.Security;
using AutoParts.Application.Features.Parts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoParts.WebApi.Controllers.V1;

[ApiController]
[Route("api/v1/parts")]
[Authorize]
public class PartsController : ControllerBase
{
    private readonly IMediator _mediator;
    public PartsController(IMediator mediator) { _mediator = mediator; }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Create([FromBody] CreatePart.Command command)
    {
        var id = await _mediator.Send(command);
        return Created($"/api/v1/parts/{id}", new { id });
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] GetParts.Query query) =>
        Ok(await _mediator.Send(query));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var part = await _mediator.Send(new GetPartById.Query { Id = id });
        return part is null ? NotFound() : Ok(part);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePart.Command command)
    {
        command.Id = id;
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _mediator.Send(new DeletePart.Command { Id = id });
        return NoContent();
    }
}
