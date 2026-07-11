namespace RetailMedia.Web.Abstractions;

public interface ICacheProvider
{
    Task<bool> IsEmptyAsync(CancellationToken cancellationToken = default);

    Task SeedCounterAsync(string key, long value, CancellationToken cancellationToken = default);

    Task IncrementAsync(string key, CancellationToken cancellationToken = default);

    Task<long?> GetCounterAsync(string key, CancellationToken cancellationToken = default);
}
