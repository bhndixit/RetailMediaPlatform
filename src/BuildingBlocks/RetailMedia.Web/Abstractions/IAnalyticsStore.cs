namespace RetailMedia.Web.Abstractions;

public interface IAnalyticsStore
{
    Task<CampaignMetricsSnapshot?> GetAsync(string tenantId, string campaignId, CancellationToken cancellationToken = default);

    Task SaveAsync(CampaignMetricsSnapshot snapshot, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CampaignMetricsSnapshot>> GetAllSnapshotsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RawEventRow>> GetRawEventsAfterAsync(DateTime from, CancellationToken cancellationToken = default);

    Task AppendRawEventAsync(RawEventRow row, CancellationToken cancellationToken = default);
}

public sealed record RawEventRow(
    string TenantId,
    string CampaignId,
    string NormalisedEventType,
    DateTime OccurredAt
);

public sealed record CampaignMetricsSnapshot(
    string CampaignId,
    string TenantId,
    long Clicks,
    long Impressions,
    long Baskets,
    decimal ClickToBasketRatio,
    DateTime LastUpdated
);
