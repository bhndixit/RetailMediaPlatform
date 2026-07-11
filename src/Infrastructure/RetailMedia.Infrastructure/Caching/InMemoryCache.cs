using System.Collections.Concurrent;
using RetailMedia.Web.Abstractions;

namespace RetailMedia.Infrastructure.Caching;

internal sealed class InMemoryCache : ICacheProvider
{
    private readonly ConcurrentDictionary<string, long> _counters = new();

    public Task<bool> IsEmptyAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_counters.IsEmpty);

    public Task SeedCounterAsync(string key, long value, CancellationToken cancellationToken = default)
    {
        _counters.AddOrUpdate(key, value, (_, existing) => existing + value);
        return Task.CompletedTask;
    }

    public Task IncrementAsync(string key, CancellationToken cancellationToken = default)
    {
        _counters.AddOrUpdate(key, 1, (_, existing) => existing + 1);
        return Task.CompletedTask;
    }

    public Task<long?> GetCounterAsync(string key, CancellationToken cancellationToken = default)
    {
        long? result = _counters.TryGetValue(key, out var value) ? value : null;
        return Task.FromResult(result);
    }
}
