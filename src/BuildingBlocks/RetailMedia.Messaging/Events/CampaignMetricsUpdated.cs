using RetailMedia.Core.Domain;

namespace RetailMedia.Messaging.Events;

public sealed record CampaignMetricsUpdated(
    Guid EventId,
    string TenantId,
    Guid CorrelationId,
    DateTime OccurredAt,
    int Version,
    string CampaignId,
    long Clicks,
    long Impressions,
    decimal ClickToBasketRatio
) : IIntegrationEvent;
