using MediatR;
using RetailMedia.Core.Domain;
using RetailMedia.Web.Abstractions;

namespace RetailMedia.Insights.Application.Queries;

internal sealed class GetCampaignClicksHandler(IAnalyticsStore analyticsStore, ICacheProvider cache)
    : IRequestHandler<GetCampaignClicksQuery, Result<CampaignClicksDto>>
{
    public async Task<Result<CampaignClicksDto>> Handle(GetCampaignClicksQuery query, CancellationToken cancellationToken)
    {
        var liveClicks = await cache.GetCounterAsync(
            $"campaign:{query.TenantId}:{query.CampaignId}:clicks", cancellationToken);

        if (liveClicks is not null)
            return Result<CampaignClicksDto>.Success(
                new CampaignClicksDto(query.CampaignId, query.TenantId, liveClicks.Value, DateTime.UtcNow));

        var snapshot = await analyticsStore.GetAsync(query.TenantId, query.CampaignId, cancellationToken);
        return snapshot is null
            ? Result<CampaignClicksDto>.Failure(Error.NotFound)
            : Result<CampaignClicksDto>.Success(
                new CampaignClicksDto(snapshot.CampaignId, snapshot.TenantId, snapshot.Clicks, snapshot.LastUpdated));
    }
}

internal sealed class GetCampaignImpressionsHandler(IAnalyticsStore analyticsStore, ICacheProvider cache)
    : IRequestHandler<GetCampaignImpressionsQuery, Result<CampaignImpressionsDto>>
{
    public async Task<Result<CampaignImpressionsDto>> Handle(GetCampaignImpressionsQuery query, CancellationToken cancellationToken)
    {
        var liveImpressions = await cache.GetCounterAsync(
            $"campaign:{query.TenantId}:{query.CampaignId}:impressions", cancellationToken);

        if (liveImpressions is not null)
            return Result<CampaignImpressionsDto>.Success(
                new CampaignImpressionsDto(query.CampaignId, query.TenantId, liveImpressions.Value, DateTime.UtcNow));

        var snapshot = await analyticsStore.GetAsync(query.TenantId, query.CampaignId, cancellationToken);
        return snapshot is null
            ? Result<CampaignImpressionsDto>.Failure(Error.NotFound)
            : Result<CampaignImpressionsDto>.Success(
                new CampaignImpressionsDto(snapshot.CampaignId, snapshot.TenantId, snapshot.Impressions, snapshot.LastUpdated));
    }
}

internal sealed class GetClickToBasketRatioHandler(IAnalyticsStore analyticsStore, ICacheProvider cache)
    : IRequestHandler<GetClickToBasketRatioQuery, Result<ClickToBasketRatioDto>>
{
    public async Task<Result<ClickToBasketRatioDto>> Handle(GetClickToBasketRatioQuery query, CancellationToken cancellationToken)
    {
        var liveClicks  = await cache.GetCounterAsync($"campaign:{query.TenantId}:{query.CampaignId}:clicks",  cancellationToken);
        var liveBaskets = await cache.GetCounterAsync($"campaign:{query.TenantId}:{query.CampaignId}:baskets", cancellationToken);

        if (liveClicks is not null && liveBaskets is not null)
        {
            var liveRatio = liveClicks.Value > 0
                ? Math.Round((decimal)liveBaskets.Value / liveClicks.Value, 4)
                : 0m;
            return Result<ClickToBasketRatioDto>.Success(
                new ClickToBasketRatioDto(query.CampaignId, query.TenantId, liveRatio, DateTime.UtcNow));
        }

        var snapshot = await analyticsStore.GetAsync(query.TenantId, query.CampaignId, cancellationToken);
        return snapshot is null
            ? Result<ClickToBasketRatioDto>.Failure(Error.NotFound)
            : Result<ClickToBasketRatioDto>.Success(
                new ClickToBasketRatioDto(snapshot.CampaignId, snapshot.TenantId, snapshot.ClickToBasketRatio, snapshot.LastUpdated));
    }
}
