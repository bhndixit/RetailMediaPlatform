using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetailMedia.Insights.Application.Queries;
using RetailMedia.Web.Extensions;

namespace RetailMedia.Insights.Api.Controllers;

[ApiController]
[Route("api/v1/ad")]
[Authorize]
public sealed class CampaignInsightsController(IMediator mediator) : ControllerBase
{
    [HttpGet("{campaignId}/clicks")]
    public async Task<IActionResult> GetClicks(string campaignId, CancellationToken cancellationToken)
    {
        var tenant = HttpContext.GetTenantContext();
        var result = await mediator.Send(new GetCampaignClicksQuery(tenant.TenantId, campaignId), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [HttpGet("{campaignId}/impressions")]
    public async Task<IActionResult> GetImpressions(string campaignId, CancellationToken cancellationToken)
    {
        var tenant = HttpContext.GetTenantContext();
        var result = await mediator.Send(new GetCampaignImpressionsQuery(tenant.TenantId, campaignId), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [HttpGet("{campaignId}/clickToBasket")]
    public async Task<IActionResult> GetClickToBasketRatio(string campaignId, CancellationToken cancellationToken)
    {
        var tenant = HttpContext.GetTenantContext();
        var result = await mediator.Send(new GetClickToBasketRatioQuery(tenant.TenantId, campaignId), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }
}
