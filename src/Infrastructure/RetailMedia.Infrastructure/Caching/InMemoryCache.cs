using System.Collections.Concurrent;
using RetailMedia.Web.Abstractions;

namespace RetailMedia.Infrastructure.Caching;

internal sealed class InMemoryCache : ICacheProvider
{
    private readonly ConcurrentDictionary<string, CacheEntry> _store = new();

    public Task InvalidateAsync(string key, CancellationToken cancellationToken = default)
    {
        _store.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    public async Task<T?> GetOrFetchAsync<T>(string key, Func<CancellationToken, Task<T?>> fetch, TimeSpan ttl, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var cached = GetFromStore<T>(key);
            if (cached is not null)
                return cached;
        }
        catch
        {
            // Cache unavailable — fall through to the fetch delegate (analytical store).
        }

        var value = await fetch(cancellationToken);
        if (value is not null)
            SetInStore(key, value, ttl);

        return value;
    }

    private T? GetFromStore<T>(string key) where T : class
    {
        if (_store.TryGetValue(key, out var entry) && entry.ExpiresAt > DateTime.UtcNow)
            return (T?)entry.Value;
        return null;
    }

    private void SetInStore<T>(string key, T value, TimeSpan ttl) where T : class =>
        _store[key] = new CacheEntry(value, DateTime.UtcNow.Add(ttl));

    private sealed record CacheEntry(object Value, DateTime ExpiresAt);
}
