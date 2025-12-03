namespace CartService.Infrastructure.Messaging
{
    public interface IMessagePublisher
    {
        /// <summary>
        /// Publish a message to Service Bus (topic or queue).
        /// </summary>
        Task PublishAsync(string destination, string messageType, string messageId, string content, CancellationToken ct = default);
    }
}
