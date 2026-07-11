using System.Collections.Concurrent;
using RetailMedia.Web.Abstractions;

namespace RetailMedia.Infrastructure.Analytics;

public sealed class InMemoryAnalyticsStore : IAnalyticsStore
{
    private readonly ConcurrentDictionary<string, CampaignMetricsSnapshot> _store = new();
    private readonly ConcurrentBag<RawEventRow> _rawEvents = [];

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

    public Task<IReadOnlyList<CampaignMetricsSnapshot>> GetAllSnapshotsAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<CampaignMetricsSnapshot> result = _store.Values.ToList();
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<RawEventRow>> GetRawEventsAfterAsync(DateTime from, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<RawEventRow> result = _rawEvents
            .Where(e => e.OccurredAt > from)
            .OrderBy(e => e.OccurredAt)
            .ToList();
        return Task.FromResult(result);
    }

    public Task AppendRawEventAsync(RawEventRow row, CancellationToken cancellationToken = default)
    {
        _rawEvents.Add(row);
        return Task.CompletedTask;
    }

    private static string BuildKey(string tenantId, string campaignId) =>
        $"{tenantId}:{campaignId}";
}
