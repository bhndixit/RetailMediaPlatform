using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RetailMedia.Messaging.Abstractions;

namespace RetailMedia.Infrastructure.Messaging;

internal sealed class InMemoryEventBus(IServiceProvider serviceProvider, ILogger<InMemoryEventBus> logger) : IEventBus
{
    private readonly ConcurrentDictionary<string, List<Type>> _subscriptions = new();

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : class
    {
        var eventName = typeof(TEvent).Name;
        logger.LogInformation("Publishing {EventName}", eventName);

        if (!_subscriptions.TryGetValue(eventName, out var handlerTypes))
            return;

        foreach (var handlerType in handlerTypes)
        {
            var handler = serviceProvider.GetRequiredService(handlerType);
            if (handler is IEventHandler<TEvent> typedHandler)
                await typedHandler.HandleAsync(@event, cancellationToken);
        }
    }

    public void Subscribe<TEvent, THandler>()
        where TEvent : class
        where THandler : IEventHandler<TEvent>
    {
        var eventName = typeof(TEvent).Name;
        _subscriptions.AddOrUpdate(
            eventName,
            [typeof(THandler)],
            (_, existing) =>
            {
                existing.Add(typeof(THandler));
                return existing;
            });
    }
}
