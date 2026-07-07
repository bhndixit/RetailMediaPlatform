using System.Collections.Concurrent;
using RetailMedia.Web.Abstractions;

namespace RetailMedia.Infrastructure.Analytics;

public sealed class InMemoryAnalyticsStore : IAnalyticsStore
{
    private readonly ConcurrentDictionary<string, CampaignMetricsSnapshot> _store = new();

    public Task<CampaignMetricsSnapshot?> GetAsync(string tenantId, string campaignId, CancellationToken cancellationToken = default)
    {
        var key = BuildKey(tenantId, campaignId);
        _store.TryGetValue(key, out var snapshot);
        return Task.FromResult(snapshot);
    }

    public Task SaveAsync(CampaignMetricsSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        var key = BuildKey(snapshot.TenantId, snapshot.CampaignId);
        _store[key] = snapshot;
        return Task.CompletedTask;
    }

    private static string BuildKey(string tenantId, string campaignId) =>
        $"{tenantId}:{campaignId}";
}
