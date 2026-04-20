using AutoParts.Application.Common.Security;
using AutoParts.Application.Features.Vendors.Commands.CreateVendor;
using AutoParts.Application.Features.Vendors.Commands.DeleteVendor;
using AutoParts.Application.Features.Vendors.Commands.UpdateVendor;
using AutoParts.Application.Features.Vendors.Queries.GetVendorById;
using AutoParts.Application.Features.Vendors.Queries.GetVendors;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoParts.WebApi.Controllers.V1;

[ApiController]
[Route("api/v1/vendors")]
[Authorize(Roles = Roles.Admin)]
public class VendorsController : ControllerBase
{
    private readonly IMediator _mediator;

    public VendorsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateVendorCommand command)
    {
        var id = await _mediator.Send(command);
        return Created($"/api/v1/vendors/{id}", new { id });
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] GetVendorsQuery query)
    {
        var page = await _mediator.Send(query);
        return Ok(page);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var vendor = await _mediator.Send(new GetVendorByIdQuery { Id = id });
        if (vendor is null) return NotFound(new { message = "Vendor not found." });
        return Ok(vendor);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateVendorCommand command)
    {
        command.Id = id;
        var ok = await _mediator.Send(command);
        if (!ok) return NotFound(new { message = "Vendor not found." });
        return Ok(new { message = "Vendor updated." });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var ok = await _mediator.Send(new DeleteVendorCommand { Id = id });
        if (!ok) return NotFound(new { message = "Vendor not found." });
        return NoContent();
    }
}
