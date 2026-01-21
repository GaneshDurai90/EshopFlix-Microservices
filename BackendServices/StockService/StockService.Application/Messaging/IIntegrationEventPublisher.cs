namespace StockService.Application.Messaging;

/// <summary>
/// Interface for publishing integration events to the outbox for reliable delivery.
/// </summary>
public interface IIntegrationEventPublisher
{
    /// <summary>
    /// Publishes an integration event to the outbox.
    /// The event will be processed asynchronously and delivered to subscribers.
    /// </summary>
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default) where TEvent : IIntegrationEvent;

    /// <summary>
    /// Publishes an event with a specific type name.
    /// </summary>
    Task PublishAsync(string eventType, object payload, CancellationToken ct = default);
}
