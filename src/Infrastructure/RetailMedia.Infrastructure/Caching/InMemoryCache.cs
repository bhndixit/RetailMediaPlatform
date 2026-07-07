using System.Collections.Concurrent;
using RetailMedia.Web.Abstractions;

namespace RetailMedia.Infrastructure.Caching;

internal sealed class InMemoryCache : ICacheProvider
{
    private readonly ConcurrentDictionary<string, CacheEntry> _store = new();

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        if (_store.TryGetValue(key, out var entry) && entry.ExpiresAt > DateTime.UtcNow)
            return Task.FromResult((T?)entry.Value);

        return Task.FromResult<T?>(null);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default) where T : class
    {
        _store[key] = new CacheEntry(value, DateTime.UtcNow.Add(ttl));
        return Task.CompletedTask;
    }

    public Task InvalidateAsync(string key, CancellationToken cancellationToken = default)
    {
        _store.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    public async Task<T?> GetOrFetchAsync<T>(string key, Func<CancellationToken, Task<T?>> fetch, TimeSpan ttl, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var cached = await GetAsync<T>(key, cancellationToken);
            if (cached is not null)
                return cached;
        }
        catch
        {
            // Cache unavailable — fall through to the fetch delegate (analytical store).
        }

        var value = await fetch(cancellationToken);
        if (value is not null)
            await SetAsync(key, value, ttl, cancellationToken);

        return value;
    }

    private sealed record CacheEntry(object Value, DateTime ExpiresAt);
}
