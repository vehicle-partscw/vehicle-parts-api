using AutoParts.Application.Common.Security;
using AutoParts.Application.Features.Vehicles;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoParts.WebApi.Controllers.V1;

[ApiController]
[Route("api/v1/vehicles")]
[Authorize]
public class VehiclesController : ControllerBase
{
    private readonly IMediator _mediator;
    public VehiclesController(IMediator mediator) { _mediator = mediator; }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateVehicle.Command command)
    {
        var id = await _mediator.Send(command);
        return Created($"/api/v1/vehicles/{id}", new { id });
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] GetVehicles.Query query) =>
        Ok(await _mediator.Send(query));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var v = await _mediator.Send(new GetVehicleById.Query { Id = id });
        return v is null ? NotFound() : Ok(v);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateVehicle.Command command)
    {
        command.Id = id;
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _mediator.Send(new DeleteVehicle.Command { Id = id });
        return NoContent();
    }
}
