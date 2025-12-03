using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CartService.Infrastructure.Messaging
{
    public static class MessagingServiceCollectionExtensions
    {
        public static IServiceCollection AddServiceBusMessaging(this IServiceCollection services, IConfiguration configuration)
        {
            var conn = configuration["ServiceBus:ConnectionString"]; // ***MASKED***
            services.AddSingleton(sp => new ServiceBusClient(conn));
            services.AddSingleton<ServiceBusProcessorFactory>();
            services.AddSingleton<IMessagePublisher, ServiceBusMessagePublisher>();
            return services;
        }

        public static IServiceCollection AddOutboxDispatcher(this IServiceCollection services)
        {
            services.AddHostedService<OutboxDispatcher>();
            return services;
        }
    }
}
