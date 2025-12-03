using Azure.Messaging.ServiceBus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace CartService.Infrastructure.Messaging
{
    public class ServiceBusMessagePublisher : IMessagePublisher, IAsyncDisposable
    {
        private readonly ServiceBusClient _client;
        private readonly ILogger _log;

        public ServiceBusMessagePublisher(ServiceBusClient client)
        {
            _client = client;
            _log = Log.ForContext<ServiceBusMessagePublisher>();
        }

        public async Task PublishAsync(string destination, string messageType, string messageId, string content, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(destination))
                throw new ArgumentException("destination is required", nameof(destination));

            ServiceBusSender sender;
            if (destination.StartsWith("topic:", StringComparison.OrdinalIgnoreCase))
            {
                var topic = destination.Substring("topic:".Length);
                sender = _client.CreateSender(topic);
            }
            else
            {
                sender = _client.CreateSender(destination);
            }

            var sbMessage = new ServiceBusMessage(content)
            {
                MessageId = messageId,
                Subject = messageType
            };

            sbMessage.ApplicationProperties["messageType"] = messageType;
            // Add correlationId when available via message headers if needed

            await sender.SendMessageAsync(sbMessage, ct);
            _log.Information("Published message {MessageId} type={Type} to {Destination}", messageId, messageType, destination);
        }

        public async ValueTask DisposeAsync()
        {
            await _client.DisposeAsync();
        }
    }
}
