using AutoParts.Application.Common.Security;
using AutoParts.Application.Features.Customers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoParts.WebApi.Controllers.V1;

[ApiController]
[Route("api/v1/customers")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Staff}")]
public class CustomersController : ControllerBase
{
    private readonly IMediator _mediator;
    public CustomersController(IMediator mediator) { _mediator = mediator; }

    [HttpGet]
    public async Task<IActionResult> List() =>
        Ok(await _mediator.Send(new GetCustomers.Query()));

    [HttpGet("{userId}")]
    public async Task<IActionResult> GetById(string userId)
    {
        var dto = await _mediator.Send(new GetCustomerById.Query { UserId = userId });
        return dto is null ? NotFound(new { message = "Customer not found." }) : Ok(dto);
    }

    [HttpPatch("{userId}/toggle-active")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> ToggleActive(string userId)
    {
        var ok = await _mediator.Send(new ToggleCustomerActive.Command { UserId = userId });
        return ok ? Ok(new { message = "Customer active status toggled." })
                  : NotFound(new { message = "Customer not found." });
    }

    public class CreditLimitRequest { public decimal? CreditLimit { get; set; } }

    [HttpPatch("{userId}/credit-limit")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> UpdateCreditLimit(string userId, [FromBody] CreditLimitRequest body)
    {
        var ok = await _mediator.Send(new UpdateCustomerCreditLimit.Command
        {
            UserId = userId,
            CreditLimit = body.CreditLimit
        });
        return ok ? Ok(new { message = "Credit limit updated." })
                  : NotFound(new { message = "Customer not found." });
    }
}
