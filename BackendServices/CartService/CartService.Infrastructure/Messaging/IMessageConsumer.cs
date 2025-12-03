namespace CartService.Infrastructure.Messaging
{
    public interface IMessageConsumer<T>
    {
        Task HandleAsync(T message, string messageId, CancellationToken ct);
    }
}
