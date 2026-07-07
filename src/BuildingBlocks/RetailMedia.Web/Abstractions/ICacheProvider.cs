namespace RetailMedia.Web.Abstractions;

public interface ICacheProvider
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default) where T : class;
    Task InvalidateAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the cached value when available. When the cache is unavailable or the entry is
    /// absent, falls back to <paramref name="fetch"/> and caches the result.
    /// This makes the Redis-unavailable fallback path explicit rather than implicit.
    /// Production: if Redis throws, fetch runs against the analytical store directly.
    /// </summary>
    Task<T?> GetOrFetchAsync<T>(string key, Func<CancellationToken, Task<T?>> fetch, TimeSpan ttl, CancellationToken cancellationToken = default) where T : class;
}
