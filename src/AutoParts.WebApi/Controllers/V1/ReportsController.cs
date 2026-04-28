using AutoParts.Application.Common.Security;
using AutoParts.Application.Features.Reports;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoParts.WebApi.Controllers.V1;

[ApiController]
[Route("api/v1/reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IMediator _mediator;
    public ReportsController(IMediator mediator) { _mediator = mediator; }

    [HttpGet("financial")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Financial([FromQuery] GetFinancialReport.Query query) =>
        Ok(await _mediator.Send(query));

    [HttpGet("customers")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Staff}")]
    public async Task<IActionResult> Customers([FromQuery] GetCustomerReport.Query query) =>
        Ok(await _mediator.Send(query));
}
