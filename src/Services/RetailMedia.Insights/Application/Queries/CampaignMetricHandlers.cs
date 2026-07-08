using MediatR;
using RetailMedia.Core.Domain;
using RetailMedia.Web.Abstractions;

namespace RetailMedia.Insights.Application.Queries;

internal sealed class GetCampaignClicksHandler(IAnalyticsStore analyticsStore, ICacheProvider cache)
    : IRequestHandler<GetCampaignClicksQuery, Result<CampaignClicksDto>>
{
    public async Task<Result<CampaignClicksDto>> Handle(GetCampaignClicksQuery query, CancellationToken cancellationToken)
    {
        var cacheKey = $"campaign:{query.TenantId}:{query.CampaignId}:clicks";

        var metrics = await cache.GetOrFetchAsync<CampaignMetricsSnapshot>(
            cacheKey,
            ct => analyticsStore.GetAsync(query.TenantId, query.CampaignId, ct),
            ttl: TimeSpan.FromSeconds(120),
            cancellationToken);

        return metrics is null
            ? Result<CampaignClicksDto>.Failure(Error.NotFound)
            : Result<CampaignClicksDto>.Success(new CampaignClicksDto(
                metrics.CampaignId, metrics.TenantId, metrics.Clicks, metrics.LastUpdated));
    }
}

internal sealed class GetCampaignImpressionsHandler(IAnalyticsStore analyticsStore, ICacheProvider cache)
    : IRequestHandler<GetCampaignImpressionsQuery, Result<CampaignImpressionsDto>>
{
    public async Task<Result<CampaignImpressionsDto>> Handle(GetCampaignImpressionsQuery query, CancellationToken cancellationToken)
    {
        var cacheKey = $"campaign:{query.TenantId}:{query.CampaignId}:impressions";

        var metrics = await cache.GetOrFetchAsync<CampaignMetricsSnapshot>(
            cacheKey,
            ct => analyticsStore.GetAsync(query.TenantId, query.CampaignId, ct),
            ttl: TimeSpan.FromSeconds(120),
            cancellationToken);

        return metrics is null
            ? Result<CampaignImpressionsDto>.Failure(Error.NotFound)
            : Result<CampaignImpressionsDto>.Success(new CampaignImpressionsDto(
                metrics.CampaignId, metrics.TenantId, metrics.Impressions, metrics.LastUpdated));
    }
}

internal sealed class GetClickToBasketRatioHandler(IAnalyticsStore analyticsStore, ICacheProvider cache)
    : IRequestHandler<GetClickToBasketRatioQuery, Result<ClickToBasketRatioDto>>
{
    public async Task<Result<ClickToBasketRatioDto>> Handle(GetClickToBasketRatioQuery query, CancellationToken cancellationToken)
    {
        var cacheKey = $"campaign:{query.TenantId}:{query.CampaignId}:clickToBasket";

        var metrics = await cache.GetOrFetchAsync<CampaignMetricsSnapshot>(
            cacheKey,
            ct => analyticsStore.GetAsync(query.TenantId, query.CampaignId, ct),
            ttl: TimeSpan.FromSeconds(120),
            cancellationToken);

        return metrics is null
            ? Result<ClickToBasketRatioDto>.Failure(Error.NotFound)
            : Result<ClickToBasketRatioDto>.Success(new ClickToBasketRatioDto(
                metrics.CampaignId, metrics.TenantId, metrics.ClickToBasketRatio, metrics.LastUpdated));
    }
}
