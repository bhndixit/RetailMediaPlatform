using RetailMedia.Core.Domain;

namespace RetailMedia.Messaging.Events;

public sealed record CustomerEventRecorded(
    Guid EventId,
    string TenantId,
    Guid CorrelationId,
    DateTime OccurredAt,
    int Version,
    Guid CustomerEventId,
    string CampaignId,
    string CustomerId,
    string EventType
) : IIntegrationEvent;
