namespace RetailMedia.Core.Domain;

public interface IIntegrationEvent
{
    Guid EventId { get; }
    string TenantId { get; }
    Guid CorrelationId { get; }
    DateTime OccurredAt { get; }
    int Version { get; }
}
