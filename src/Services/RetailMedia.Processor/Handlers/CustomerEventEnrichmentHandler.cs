using Microsoft.Extensions.Logging;
using RetailMedia.Messaging.Abstractions;
using RetailMedia.Messaging.Events;

namespace RetailMedia.Processor.Handlers;

public sealed class CustomerEventEnrichmentHandler(
    IEventBus eventBus,
    ILogger<CustomerEventEnrichmentHandler> logger)
    : IEventHandler<CustomerEventRecorded>
{
    public async Task HandleAsync(CustomerEventRecorded @event, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Processing event {EventId} for campaign {CampaignId} | Tenant: {TenantId} | Correlation: {CorrelationId}",
            @event.EventId, @event.CampaignId, @event.TenantId, @event.CorrelationId);

        var normalisedEventType = @event.EventType.Trim().ToLowerInvariant();

        var processed = new CustomerEventProcessed(
            EventId: Guid.NewGuid(),
            TenantId: @event.TenantId,
            CorrelationId: @event.CorrelationId,
            OccurredAt: @event.OccurredAt,
            Version: 1,
            CustomerEventId: @event.CustomerEventId,
            CampaignId: @event.CampaignId,
            NormalisedEventType: normalisedEventType,
            ProcessedAt: DateTime.UtcNow,
            SourceTag: "collector-v1"
        );

        await eventBus.PublishAsync(processed, cancellationToken);

        logger.LogInformation(
            "Event {EventId} processed and forwarded as {ProcessedEventId}",
            @event.EventId, processed.EventId);
    }
}
