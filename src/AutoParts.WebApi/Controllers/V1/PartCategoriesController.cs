using AutoParts.Application.Common.Security;
using AutoParts.Application.Features.PartCategories;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoParts.WebApi.Controllers.V1;

[ApiController]
[Route("api/v1/part-categories")]
[Authorize(Roles = Roles.Admin)]
public class PartCategoriesController : ControllerBase
{
    private readonly IMediator _mediator;
    public PartCategoriesController(IMediator mediator) { _mediator = mediator; }

    [HttpPost]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Staff}")]
    public async Task<IActionResult> Create([FromBody] CreatePartCategory.Command command)
    {
        var id = await _mediator.Send(command);
        return Created($"/api/v1/part-categories/{id}", new { id });
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> List([FromQuery] GetPartCategories.Query query) =>
        Ok(await _mediator.Send(query));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var dto = await _mediator.Send(new GetPartCategoryById.Query { Id = id });
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePartCategory.Command command)
    {
        command.Id = id;
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _mediator.Send(new DeletePartCategory.Command { Id = id });
        return NoContent();
    }
}
