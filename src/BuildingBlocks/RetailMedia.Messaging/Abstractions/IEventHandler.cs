namespace RetailMedia.Messaging.Abstractions;

public interface IEventHandler<in TEvent>
{
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
}
