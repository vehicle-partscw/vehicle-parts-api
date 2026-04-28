using AutoParts.Application.Common.Security;
using AutoParts.Application.Features.SalesInvoices;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoParts.WebApi.Controllers.V1;

[ApiController]
[Route("api/v1/sales-invoices")]
[Authorize]
public class SalesInvoicesController : ControllerBase
{
    private readonly IMediator _mediator;
    public SalesInvoicesController(IMediator mediator) { _mediator = mediator; }

    [HttpPost]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Staff}")]
    public async Task<IActionResult> Create([FromBody] CreateSalesInvoice.Command command)
    {
        var id = await _mediator.Send(command);
        return Created($"/api/v1/sales-invoices/{id}", new { id });
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] GetSalesInvoices.Query query) =>
        Ok(await _mediator.Send(query));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var dto = await _mediator.Send(new GetSalesInvoiceById.Query { Id = id });
        return dto is null ? NotFound() : Ok(dto);
    }
}
