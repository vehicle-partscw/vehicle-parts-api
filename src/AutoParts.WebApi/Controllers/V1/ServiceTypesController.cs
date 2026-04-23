using AutoParts.Application.Common.Security;
using AutoParts.Application.Features.ServiceTypes;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoParts.WebApi.Controllers.V1;

[ApiController]
[Route("api/v1/service-types")]
[Authorize]
public class ServiceTypesController : ControllerBase
{
    private readonly IMediator _mediator;
    public ServiceTypesController(IMediator mediator) { _mediator = mediator; }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Create([FromBody] CreateServiceType.Command command)
    {
        var id = await _mediator.Send(command);
        return Created($"/api/v1/service-types/{id}", new { id });
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] GetServiceTypes.Query query) =>
        Ok(await _mediator.Send(query));

    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateServiceType.Command command)
    {
        command.Id = id;
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _mediator.Send(new DeleteServiceType.Command { Id = id });
        return NoContent();
    }
}
