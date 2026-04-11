using AutoParts.Application.Features.Staff.Commands.CreateStaff;
using AutoParts.Application.Features.Staff.Commands.ToggleStaffActive;
using AutoParts.Application.Features.Staff.Commands.UpdateStaffRole;
using AutoParts.Application.Features.Staff.Queries.GetAllStaff;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoParts.WebApi.Controllers.V1;

[ApiController]
[Route("api/v1/staff")]
[Authorize(Roles = "Admin")]
public class StaffController : ControllerBase
{
    private readonly IMediator _mediator;

    public StaffController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Register a new staff member (Admin only).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateStaff([FromBody] CreateStaffCommand command)
    {
        var result = await _mediator.Send(command);

        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors });
        }

        return Created($"/api/v1/staff/{result.UserId}", new
        {
            result.UserId,
            message = "Staff member created successfully."
        });
    }

    /// <summary>
    /// Get all staff members (Admin only).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllStaff()
    {
        var staff = await _mediator.Send(new GetAllStaffQuery());
        return Ok(staff);
    }

    /// <summary>
    /// Update a staff member's role (Admin only).
    /// </summary>
    [HttpPatch("{userId}/role")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRole(string userId, [FromBody] UpdateStaffRoleRequest request)
    {
        var command = new UpdateStaffRoleCommand { UserId = userId, NewRole = request.NewRole };
        var success = await _mediator.Send(command);

        if (!success)
        {
            return NotFound(new { message = "Staff member not found." });
        }

        return Ok(new { message = "Role updated successfully." });
    }

    /// <summary>
    /// Toggle a staff member's active status (Admin only).
    /// </summary>
    [HttpPatch("{userId}/toggle-active")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleActive(string userId)
    {
        var command = new ToggleStaffActiveCommand { UserId = userId };
        var success = await _mediator.Send(command);

        if (!success)
        {
            return NotFound(new { message = "Staff member not found." });
        }

        return Ok(new { message = "Staff active status toggled successfully." });
    }
}

public class UpdateStaffRoleRequest
{
    public string NewRole { get; set; } = string.Empty;
}
