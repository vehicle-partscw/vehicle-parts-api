using AutoParts.Application.Common.Security;
using AutoParts.Application.Features.PurchaseInvoices;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoParts.WebApi.Controllers.V1;

[ApiController]
[Route("api/v1/purchase-invoices")]
[Authorize(Roles = Roles.Admin)]
public class PurchaseInvoicesController : ControllerBase
{
    private readonly IMediator _mediator;
    public PurchaseInvoicesController(IMediator mediator) { _mediator = mediator; }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePurchaseInvoice.Command command)
    {
        var id = await _mediator.Send(command);
        return Created($"/api/v1/purchase-invoices/{id}", new { id });
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] GetPurchaseInvoices.Query query) =>
        Ok(await _mediator.Send(query));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var dto = await _mediator.Send(new GetPurchaseInvoiceById.Query { Id = id });
        return dto is null ? NotFound() : Ok(dto);
    }
}
