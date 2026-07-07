namespace RetailMedia.Web.Abstractions;

public interface IAnalyticsStore
{
    Task<CampaignMetricsSnapshot?> GetAsync(string tenantId, string campaignId, CancellationToken cancellationToken = default);
    Task SaveAsync(CampaignMetricsSnapshot snapshot, CancellationToken cancellationToken = default);
}

public sealed record CampaignMetricsSnapshot(
    string CampaignId,
    string TenantId,
    long Clicks,
    long Impressions,
    decimal ClickToBasketRatio,
    DateTime LastUpdated
);
