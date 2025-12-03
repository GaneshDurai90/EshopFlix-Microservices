using Azure.Messaging.ServiceBus;
using Serilog;

namespace CartService.Infrastructure.Messaging
{
    public class ServiceBusProcessorFactory : IAsyncDisposable
    {
        private readonly ServiceBusClient _client;
        private readonly ILogger _log;

        public ServiceBusProcessorFactory(ServiceBusClient client)
        {
            _client = client;
            _log = Log.ForContext<ServiceBusProcessorFactory>();
        }

        public ServiceBusProcessor CreateProcessor(string topicOrQueue, string subscription = null, ServiceBusProcessorOptions options = null)
        {
            options ??= new ServiceBusProcessorOptions
            {
                MaxConcurrentCalls = 5,
                AutoCompleteMessages = false,
                ReceiveMode = ServiceBusReceiveMode.PeekLock
            };

            if (!string.IsNullOrEmpty(subscription))
            {
                _log.Information("Creating topic processor {Topic} subscription {Subscription}", topicOrQueue, subscription);
                return _client.CreateProcessor(topicOrQueue, subscription, options);
            }

            _log.Information("Creating queue processor {Queue}", topicOrQueue);
            return _client.CreateProcessor(topicOrQueue, options);
        }

        public async ValueTask DisposeAsync() => await _client.DisposeAsync();
    }
}
