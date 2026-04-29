using AutoParts.Application.Common.Security;
using AutoParts.Application.Features.Engagement;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoParts.WebApi.Controllers.V1;

[ApiController]
[Route("api/v1/part-requests")]
[Authorize]
public class PartRequestsController : ControllerBase
{
    private readonly IMediator _mediator;
    public PartRequestsController(IMediator mediator) { _mediator = mediator; }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePartRequest.Command command)
    {
        var id = await _mediator.Send(command);
        return Created($"/api/v1/part-requests/{id}", new { id });
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] GetPartRequests.Query query) =>
        Ok(await _mediator.Send(query));

    /// <summary>Source a request: create a Part in the catalog and notify the customer.</summary>
    [HttpPost("{id:guid}/source")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Staff}")]
    public async Task<IActionResult> Source(Guid id, [FromBody] SourcePartRequest.Command command)
    {
        command.Id = id;
        await _mediator.Send(command);
        return NoContent();
    }

    /// <summary>Reject a request and notify the customer with an optional reason.</summary>
    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Staff}")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectPartRequest.Command command)
    {
        command.Id = id;
        await _mediator.Send(command);
        return NoContent();
    }
}

[ApiController]
[Route("api/v1/reviews")]
[Authorize]
public class ReviewsController : ControllerBase
{
    private readonly IMediator _mediator;
    public ReviewsController(IMediator mediator) { _mediator = mediator; }

    [HttpPost]
    [Authorize(Roles = Roles.Customer)]
    public async Task<IActionResult> Create([FromBody] CreateReview.Command command)
    {
        var id = await _mediator.Send(command);
        return Created($"/api/v1/reviews/{id}", new { id });
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> List([FromQuery] GetReviews.Query query) =>
        Ok(await _mediator.Send(query));
}

[ApiController]
[Route("api/v1/me/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly IMediator _mediator;
    public NotificationsController(IMediator mediator) { _mediator = mediator; }

    [HttpGet]
    public async Task<IActionResult> Mine([FromQuery] GetMyNotifications.Query query) =>
        Ok(await _mediator.Send(query));

    [HttpGet("unread-count")]
    public async Task<IActionResult> UnreadCount() =>
        Ok(new { count = await _mediator.Send(new GetMyUnreadCount.Query()) });

    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id)
    {
        await _mediator.Send(new MarkNotificationRead.Command { Id = id });
        return NoContent();
    }

    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        var count = await _mediator.Send(new MarkAllNotificationsRead.Command());
        return Ok(new { marked = count });
    }
}

[ApiController]
[Route("api/v1/me/ai-predictions")]
[Authorize(Roles = Roles.Customer)]
public class AiPredictionsController : ControllerBase
{
    private readonly IMediator _mediator;
    public AiPredictionsController(IMediator mediator) { _mediator = mediator; }

    [HttpGet]
    public async Task<IActionResult> Mine([FromQuery] GetMyAiPredictions.Query query) =>
        Ok(await _mediator.Send(query));
}
