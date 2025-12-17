using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using CatalogService.Application.Messaging;
using CatalogService.Domain.Entities;
using CatalogService.Infrastructure.Persistence;

namespace CatalogService.Infrastructure.Messaging
{
    public sealed class OutboxIntegrationEventPublisher : IIntegrationEventPublisher
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private readonly CatalogServiceDbContext _dbContext;

        public OutboxIntegrationEventPublisher(CatalogServiceDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task EnqueueAsync(string eventType, object payload, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(eventType))
            {
                throw new ArgumentException("Event type is required", nameof(eventType));
            }

            if (payload is null)
            {
                throw new ArgumentNullException(nameof(payload));
            }

            var outboxEntry = new Outbox
            {
                EventType = eventType,
                Payload = JsonSerializer.Serialize(payload, payload.GetType(), SerializerOptions),
                OccurredOn = DateTime.UtcNow,
                Processed = false,
                RetryCount = 0
            };

            await _dbContext.Outboxes.AddAsync(outboxEntry, ct);
            await _dbContext.SaveChangesAsync(ct);
        }
    }
}
