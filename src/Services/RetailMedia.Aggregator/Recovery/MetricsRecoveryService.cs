using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RetailMedia.Aggregator.Handlers;
using RetailMedia.Messaging.Abstractions;
using RetailMedia.Messaging.Events;
using RetailMedia.Web.Abstractions;

namespace RetailMedia.Aggregator.Recovery;

public sealed class MetricsRecoveryService(
    ICacheProvider cache,
    IAnalyticsStore analyticsStore,
    IEventBus eventBus,
    ILogger<MetricsRecoveryService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!await cache.IsEmptyAsync(cancellationToken))
        {
            logger.LogInformation("Cache is warm — skipping recovery.");
            SubscribeHandlers();
            return;
        }

        logger.LogWarning("Cache is empty. Seeding counters from BigQuery snapshots.");

        var snapshots = await analyticsStore.GetAllSnapshotsAsync(cancellationToken);

        if (snapshots.Count == 0)
        {
            logger.LogInformation("No snapshots found — first ever startup, starting from zero.");
            SubscribeHandlers();
            return;
        }

        foreach (var snapshot in snapshots)
        {
            await cache.SeedCounterAsync(CampaignMetricsAggregationHandler.ClickKey(snapshot.TenantId, snapshot.CampaignId),      snapshot.Clicks,      cancellationToken);
            await cache.SeedCounterAsync(CampaignMetricsAggregationHandler.ImpressionKey(snapshot.TenantId, snapshot.CampaignId), snapshot.Impressions, cancellationToken);
            await cache.SeedCounterAsync(CampaignMetricsAggregationHandler.BasketKey(snapshot.TenantId, snapshot.CampaignId),     snapshot.Baskets,     cancellationToken);

            logger.LogInformation(
                "Seeded campaign {CampaignId} | Tenant {TenantId}: Clicks={Clicks}, Impressions={Impressions}, Baskets={Baskets}",
                snapshot.CampaignId, snapshot.TenantId, snapshot.Clicks, snapshot.Impressions, snapshot.Baskets);
        }

        logger.LogInformation("Recovery complete. {Count} campaigns seeded. Subscribing to live events.", snapshots.Count);

        SubscribeHandlers();
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private void SubscribeHandlers() =>
        eventBus.Subscribe<CustomerEventProcessed, CampaignMetricsAggregationHandler>();
}
