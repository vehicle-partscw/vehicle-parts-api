using AutoParts.Application.Features.Appointments;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoParts.WebApi.Controllers.V1;

[ApiController]
[Route("api/v1/appointments")]
[Authorize]
public class AppointmentsController : ControllerBase
{
    private readonly IMediator _mediator;
    public AppointmentsController(IMediator mediator) { _mediator = mediator; }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAppointment.Command command)
    {
        var id = await _mediator.Send(command);
        return Created($"/api/v1/appointments/{id}", new { id });
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] GetAppointments.Query query) =>
        Ok(await _mediator.Send(query));

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateAppointmentStatus.Command command)
    {
        command.Id = id;
        await _mediator.Send(command);
        return Ok(new { message = "Status updated." });
    }
}
