using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using StockService.Application.Messaging;
using StockService.Domain.Entities;
using StockService.Infrastructure.Persistence;

namespace StockService.Infrastructure.Messaging;

/// <summary>
/// Outbox pattern implementation for reliable event publishing.
/// Events are stored in the OutboxMessages table and processed asynchronously.
/// </summary>
public sealed class OutboxIntegrationEventPublisher : IIntegrationEventPublisher
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    private readonly StockServiceDbContext _dbContext;
    private readonly ILogger<OutboxIntegrationEventPublisher> _logger;

    public OutboxIntegrationEventPublisher(
        StockServiceDbContext dbContext,
        ILogger<OutboxIntegrationEventPublisher> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default) 
        where TEvent : IIntegrationEvent
    {
        if (@event is null)
            throw new ArgumentNullException(nameof(@event));

        var eventType = @event.EventType;
        var payload = JsonSerializer.Serialize(@event, @event.GetType(), SerializerOptions);

        await EnqueueOutboxMessageAsync(eventType, payload, @event.EventId.ToString(), ct);

        _logger.LogInformation("Published event {EventType} with Id {EventId}", eventType, @event.EventId);
    }

    public async Task PublishAsync(string eventType, object payload, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(eventType))
            throw new ArgumentException("Event type is required", nameof(eventType));

        if (payload is null)
            throw new ArgumentNullException(nameof(payload));

        var serializedPayload = JsonSerializer.Serialize(payload, payload.GetType(), SerializerOptions);

        await EnqueueOutboxMessageAsync(eventType, serializedPayload, null, ct);

        _logger.LogInformation("Published event {EventType}", eventType);
    }

    private async Task EnqueueOutboxMessageAsync(
        string messageType, 
        string payload, 
        string? correlationId,
        CancellationToken ct)
    {
        var headers = new Dictionary<string, string>
        {
            ["Content-Type"] = "application/json",
            ["Timestamp"] = DateTime.UtcNow.ToString("O")
        };

        if (!string.IsNullOrEmpty(correlationId))
        {
            headers["Correlation-Id"] = correlationId;
        }

        var outboxMessage = new OutboxMessage
        {
            OutboxMessageId = Guid.NewGuid(),
            MessageType = messageType,
            Payload = payload,
            Headers = JsonSerializer.Serialize(headers, SerializerOptions),
            CreatedAt = DateTime.UtcNow,
            ProcessedAt = null,
            Error = null,
            Retries = 0
        };

        await _dbContext.OutboxMessages.AddAsync(outboxMessage, ct);
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogDebug("Enqueued outbox message {MessageId} of type {MessageType}", 
            outboxMessage.OutboxMessageId, messageType);
    }
}
