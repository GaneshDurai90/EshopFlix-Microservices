namespace CatalogService.Application.Messaging
{
    public interface IIntegrationEventPublisher
    {
        Task EnqueueAsync(string eventType, object payload, CancellationToken ct = default);
    }
}
