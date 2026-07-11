using Microsoft.Extensions.Logging;
using RetailMedia.Messaging.Abstractions;
using RetailMedia.Messaging.Events;
using RetailMedia.Web.Abstractions;
using RetailMedia.Web.Domain;

namespace RetailMedia.Aggregator.Handlers;

public sealed class CampaignMetricsAggregationHandler(
    IAnalyticsStore analyticsStore,
    ICacheProvider cache,
    IEventBus eventBus,
    ILogger<CampaignMetricsAggregationHandler> logger)
    : IEventHandler<CustomerEventProcessed>
{
    public async Task HandleAsync(CustomerEventProcessed @event, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Aggregating metrics for campaign {CampaignId} | EventType: {EventType} | Tenant: {TenantId}",
            @event.CampaignId, @event.NormalisedEventType, @event.TenantId);

        var counterKey = @event.NormalisedEventType switch
        {
            EventType.Click      => ClickKey(@event.TenantId, @event.CampaignId),
            EventType.Impression => ImpressionKey(@event.TenantId, @event.CampaignId),
            EventType.Basket     => BasketKey(@event.TenantId, @event.CampaignId),
            _ => null
        };

        if (counterKey is null)
        {
            logger.LogWarning(
                "Unrecognised event type '{EventType}' for campaign {CampaignId} — metrics unchanged.",
                @event.NormalisedEventType, @event.CampaignId);
            return;
        }

        await cache.IncrementAsync(counterKey, cancellationToken);

        var clicks      = await cache.GetCounterAsync(ClickKey(@event.TenantId, @event.CampaignId), cancellationToken) ?? 0;
        var impressions = await cache.GetCounterAsync(ImpressionKey(@event.TenantId, @event.CampaignId), cancellationToken) ?? 0;
        var baskets     = await cache.GetCounterAsync(BasketKey(@event.TenantId, @event.CampaignId), cancellationToken) ?? 0;
        var ratio       = clicks > 0 ? Math.Round((decimal)baskets / clicks, 4) : 0m;

        await analyticsStore.SaveAsync(new CampaignMetricsSnapshot(
            @event.CampaignId,
            @event.TenantId,
            clicks,
            impressions,
            baskets,
            ratio,
            DateTime.UtcNow), cancellationToken);

        await eventBus.PublishAsync(new CampaignMetricsUpdated(
            EventId: Guid.NewGuid(),
            TenantId: @event.TenantId,
            CorrelationId: @event.CorrelationId,
            OccurredAt: DateTime.UtcNow,
            Version: 1,
            CampaignId: @event.CampaignId,
            Clicks: clicks,
            Impressions: impressions,
            Baskets: baskets,
            ClickToBasketRatio: ratio
        ), cancellationToken);

        logger.LogInformation(
            "Metrics updated for campaign {CampaignId}: Clicks={Clicks}, Impressions={Impressions}, Baskets={Baskets}",
            @event.CampaignId, clicks, impressions, baskets);
    }

    internal static string ClickKey(string tenantId, string campaignId) =>
        $"campaign:{tenantId}:{campaignId}:clicks";

    internal static string ImpressionKey(string tenantId, string campaignId) =>
        $"campaign:{tenantId}:{campaignId}:impressions";

    internal static string BasketKey(string tenantId, string campaignId) =>
        $"campaign:{tenantId}:{campaignId}:baskets";
}
