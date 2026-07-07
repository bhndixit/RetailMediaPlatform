using RetailMedia.Core.Domain;

namespace RetailMedia.Messaging.Events;

public sealed record CustomerEventProcessed(
    Guid EventId,
    string TenantId,
    Guid CorrelationId,
    DateTime OccurredAt,
    int Version,
    Guid CustomerEventId,
    string CampaignId,
    string NormalisedEventType,
    DateTime ProcessedAt,
    string SourceTag
) : IIntegrationEvent;
