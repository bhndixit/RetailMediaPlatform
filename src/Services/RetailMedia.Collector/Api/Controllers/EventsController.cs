using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetailMedia.Collector.Application.Commands;
using RetailMedia.Web.Extensions;

namespace RetailMedia.Collector.Api.Controllers;

[ApiController]
[Route("api/v1/events")]
[Authorize]
public sealed class EventsController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Record([FromBody] RecordEventRequest request, CancellationToken cancellationToken)
    {
        var tenant = HttpContext.GetTenantContext();

        var command = new RecordCustomerEventCommand(
            tenant.TenantId,
            tenant.CorrelationId,
            request.CampaignId,
            request.CustomerId,
            request.EventType);

        var result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? Accepted(new { EventId = result.Value })
            : BadRequest(result.Error);
    }
}

public sealed record RecordEventRequest(string CampaignId, string CustomerId, string EventType);
