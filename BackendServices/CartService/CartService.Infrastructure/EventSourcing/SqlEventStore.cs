using System.Text.Json;
using CartService.Domain.Events;
using CartService.Domain.Entities;
using CartService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Serilog;
using CartService.Application.EventSourcing;

namespace CartService.Infrastructure.EventSourcing
{
    public sealed class SqlEventStore : IEventStore
    {
        private readonly CartServiceDbContext _db;
        private readonly JsonSerializerOptions _json = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        public SqlEventStore(CartServiceDbContext db) => _db = db;

        public async Task<IReadOnlyList<IDomainEvent>> LoadAsync(long cartId, CancellationToken ct = default)
        {
            var rows = await _db.CartEvents
                .AsNoTracking()
                .Where(e => e.CartId == cartId)
                .OrderBy(e => e.Id)
                .ToListAsync(ct);

            var list = new List<IDomainEvent>(rows.Count);
            foreach (var row in rows)
            {
                try
                {
                    var evt = Deserialize(row.EventType, row.DataJson);
                    if (evt is not null) list.Add(evt);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to deserialize event {EventType} for CartId={CartId} RowId={RowId}", row.EventType, row.CartId, row.Id);
                }
            }
            return list;
        }

        public async Task<int> AppendAsync(long cartId, IEnumerable<IDomainEvent> events, string? causedBy, CancellationToken ct = default)
        {
            var currentVersion = await _db.CartEvents.Where(x => x.CartId == cartId).CountAsync(ct);
            var version = currentVersion;

            foreach (var e in events)
            {
                version++;
                var evtWithVersion = WithVersion(e, version, causedBy);
                _db.CartEvents.Add(new CartEvent
                {
                    CartId = cartId,
                    EventType = evtWithVersion.GetType().Name,
                    DataJson = JsonSerializer.Serialize(evtWithVersion, evtWithVersion.GetType(), _json),
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = causedBy
                });
            }

            await _db.SaveChangesAsync(ct);
            return version;
        }

        private static IDomainEvent WithVersion(IDomainEvent e, int version, string? causedBy)
        {
            var now = DateTime.UtcNow;
            return e switch
            {
                CartCreatedV1 x => x with { Version = version, OccurredOnUtc = now, CausedBy = causedBy },
                ItemAddedV1 x => x with { Version = version, OccurredOnUtc = now, CausedBy = causedBy },
                ItemQuantityUpdatedV1 x => x with { Version = version, OccurredOnUtc = now, CausedBy = causedBy },
                ItemRemovedV1 x => x with { Version = version, OccurredOnUtc = now, CausedBy = causedBy },
                CouponAppliedV1 x => x with { Version = version, OccurredOnUtc = now, CausedBy = causedBy },
                CouponRemovedV1 x => x with { Version = version, OccurredOnUtc = now, CausedBy = causedBy },
                ShippingSelectedV1 x => x with { Version = version, OccurredOnUtc = now, CausedBy = causedBy },
                TotalsRecalculatedV1 x => x with { Version = version, OccurredOnUtc = now, CausedBy = causedBy },
                CartClearedV1 x => x with { Version = version, OccurredOnUtc = now, CausedBy = causedBy },
                ItemSavedForLaterV1 x => x with { Version = version, OccurredOnUtc = now, CausedBy = causedBy },
                SavedItemMovedToCartV1 x => x with { Version = version, OccurredOnUtc = now, CausedBy = causedBy },
                PaymentSetV1 x => x with { Version = version, OccurredOnUtc = now, CausedBy = causedBy },
                CartDeactivatedV1 x => x with { Version = version, OccurredOnUtc = now, CausedBy = causedBy },
                CartSnapshotV1 x => x with { Version = version, OccurredOnUtc = now, CausedBy = causedBy },
                _ => e switch
                {
                    CartEventBase b => b with { Version = version, OccurredOnUtc = now, CausedBy = causedBy },
                    _ => e
                }
            };
        }

        private static IDomainEvent? Deserialize(string eventType, string json)
        {
            var t = Type.GetType($"CartService.Domain.Events.{eventType}, CartService.Domain");
            return t is null ? null : JsonSerializer.Deserialize(json, t) as IDomainEvent;
        }
    }
}
