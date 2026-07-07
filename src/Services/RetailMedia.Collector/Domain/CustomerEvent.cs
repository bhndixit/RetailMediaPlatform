using RetailMedia.Core.Domain;

namespace RetailMedia.Collector.Domain;

public sealed class CustomerEvent : Entity<Guid>
{
    public string CampaignId { get; private set; } = string.Empty;
    public string CustomerId { get; private set; } = string.Empty;
    public string EventType { get; private set; } = string.Empty;
    public DateTime OccurredAt { get; private set; }
    public Guid CorrelationId { get; private set; }

    private CustomerEvent() { }

    public static CustomerEvent Record(
        string tenantId,
        string campaignId,
        string customerId,
        string eventType,
        Guid correlationId)
    {
        var customerEvent = new CustomerEvent
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CampaignId = campaignId,
            CustomerId = customerId,
            EventType = eventType,
            OccurredAt = DateTime.UtcNow,
            CorrelationId = correlationId,
            CreatedAt = DateTime.UtcNow
        };

        return customerEvent;
    }
}
