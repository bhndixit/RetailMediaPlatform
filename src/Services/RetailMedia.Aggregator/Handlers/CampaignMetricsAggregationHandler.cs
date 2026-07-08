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

        var current = await analyticsStore.GetAsync(@event.TenantId, @event.CampaignId, cancellationToken)
            ?? new CampaignMetricsSnapshot(@event.CampaignId, @event.TenantId, 0, 0, 0, 0m, DateTime.UtcNow);

        var updated = @event.NormalisedEventType switch
        {
            EventType.Click      => current with { Clicks = current.Clicks + 1, LastUpdated = DateTime.UtcNow },
            EventType.Impression => current with { Impressions = current.Impressions + 1, LastUpdated = DateTime.UtcNow },
            EventType.Basket     => current with { Baskets = current.Baskets + 1, LastUpdated = DateTime.UtcNow },
            _ => LogAndReturnUnchanged(current, @event.NormalisedEventType, @event.CampaignId)
        };

        // ClickToBasket = sessions with a basket event / sessions with a click event (true conversion ratio)
        var clickToBasketRatio = updated.Clicks > 0
            ? Math.Round((decimal)updated.Baskets / updated.Clicks, 4)
            : 0m;

        updated = updated with { ClickToBasketRatio = clickToBasketRatio };

        await analyticsStore.SaveAsync(updated, cancellationToken);

        // Invalidate all cached metric views for this campaign so Insights serves fresh data.
        // Ownership of cache invalidation sits here — the writer is responsible, not the reader.
        await cache.InvalidateAsync($"campaign:{@event.TenantId}:{@event.CampaignId}:clicks", cancellationToken);
        await cache.InvalidateAsync($"campaign:{@event.TenantId}:{@event.CampaignId}:impressions", cancellationToken);
        await cache.InvalidateAsync($"campaign:{@event.TenantId}:{@event.CampaignId}:clickToBasket", cancellationToken);

        await eventBus.PublishAsync(new CampaignMetricsUpdated(
            EventId: Guid.NewGuid(),
            TenantId: @event.TenantId,
            CorrelationId: @event.CorrelationId,
            OccurredAt: DateTime.UtcNow,
            Version: 1,
            CampaignId: @event.CampaignId,
            Clicks: updated.Clicks,
            Impressions: updated.Impressions,
            Baskets: updated.Baskets,
            ClickToBasketRatio: updated.ClickToBasketRatio
        ), cancellationToken);

        logger.LogInformation(
            "Metrics updated for campaign {CampaignId}: Clicks={Clicks}, Impressions={Impressions}, Baskets={Baskets}",
            updated.CampaignId, updated.Clicks, updated.Impressions, updated.Baskets);
    }

    private CampaignMetricsSnapshot LogAndReturnUnchanged(
        CampaignMetricsSnapshot current, string eventType, string campaignId)
    {
        logger.LogWarning(
            "Unrecognised event type '{EventType}' for campaign {CampaignId} — metrics unchanged.",
            eventType, campaignId);
        return current;
    }
}
