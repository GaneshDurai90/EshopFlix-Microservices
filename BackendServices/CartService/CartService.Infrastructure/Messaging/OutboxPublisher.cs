using System;
using System.Text.Json;
using CartService.Application.Messaging;
using CartService.Domain.Entities;
using CartService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CartService.Infrastructure.Messaging
{
    public sealed class OutboxPublisher : IOutboxPublisher
    {
        private readonly CartServiceDbContext _db;
        private readonly JsonSerializerOptions _json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        public OutboxPublisher(CartServiceDbContext db) => _db = db;

        public async Task EnqueueAsync(string type, object payload, string? destination = null, CancellationToken ct = default)
        {
            var msg = new OutboxMessage
            {
                MessageId = Guid.NewGuid().ToString("N"),
                Type = type,
                Content = JsonSerializer.Serialize(payload, payload.GetType(), _json),
                Destination = destination,
                Processed = false,
                OccurredOn = DateTime.UtcNow
            };
            _db.OutboxMessages.Add(msg);
            await _db.SaveChangesAsync(ct);
        }
    }
}
