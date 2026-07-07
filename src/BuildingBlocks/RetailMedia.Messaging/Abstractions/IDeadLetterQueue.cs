namespace RetailMedia.Messaging.Abstractions;

/// <summary>
/// Captures events that have exceeded their retry limit.
/// Production implementation: Google Pub/Sub Dead Letter Topic.
/// </summary>
public interface IDeadLetterQueue<TEvent> where TEvent : class
{
    Task EnqueueAsync(TEvent failedEvent, string failureReason, int attemptCount, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DeadLetterEntry<TEvent>>> ReadAsync(int maxCount, CancellationToken cancellationToken = default);
}

public sealed record DeadLetterEntry<TEvent>(
    TEvent Event,
    string FailureReason,
    int AttemptCount,
    DateTime FailedAt
);
