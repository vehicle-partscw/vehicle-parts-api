using AutoParts.Application.Common.Security;
using AutoParts.Application.Features.Payments;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoParts.WebApi.Controllers.V1;

[ApiController]
[Route("api/v1/payments")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Staff}")]
public class PaymentsController : ControllerBase
{
    private readonly IMediator _mediator;
    public PaymentsController(IMediator mediator) { _mediator = mediator; }

    [HttpPost]
    public async Task<IActionResult> Record([FromBody] RecordPayment.Command command)
    {
        var id = await _mediator.Send(command);
        return Created($"/api/v1/payments/{id}", new { id });
    }

    [HttpGet("invoice/{salesInvoiceId:guid}")]
    public async Task<IActionResult> ForInvoice(Guid salesInvoiceId) =>
        Ok(await _mediator.Send(new GetPaymentsForInvoice.Query { SalesInvoiceId = salesInvoiceId }));
}
