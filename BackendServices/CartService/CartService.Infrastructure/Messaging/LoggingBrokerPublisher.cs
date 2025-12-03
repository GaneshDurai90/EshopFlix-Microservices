using CartService.Application.Messaging;
using Serilog;

namespace CartService.Infrastructure.Messaging
{
    // Default broker publisher that just logs. Replace with MassTransit/AzureSB/Kafka publisher later.
    public sealed class LoggingBrokerPublisher : IBrokerPublisher
    {
        public Task PublishAsync(string type, string destination, string content, CancellationToken ct = default)
        {
            Log.Information("Publishing integration event Type={Type} Destination={Destination} Payload={Payload}", type, destination, content);
            return Task.CompletedTask;
        }
    }
}
