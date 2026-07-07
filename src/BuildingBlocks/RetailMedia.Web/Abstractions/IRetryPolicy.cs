namespace RetailMedia.Web.Abstractions;

/// <summary>
/// Executes an operation with a configurable retry strategy.
/// Production implementation: Polly resilience pipeline.
/// </summary>
public interface IRetryPolicy
{
    Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default);
    Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default);
}

public sealed record RetryOptions(
    int MaxAttempts,
    TimeSpan InitialDelay,
    bool UseExponentialBackoff
);
