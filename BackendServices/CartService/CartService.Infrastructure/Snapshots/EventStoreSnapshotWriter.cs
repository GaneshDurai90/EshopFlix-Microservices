using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using CartService.Application.EventSourcing;
using CartService.Application.Snapshots;
using CartService.Domain.Events;
using CartService.Infrastructure.Persistence;
using CartService.Application.Repositories;

namespace CartService.Infrastructure.Snapshots
{
    public sealed class EventStoreSnapshotWriter : ISnapshotWriter
    {
        private readonly IEventStore _eventStore;
        private readonly ICartRepository _repo;

        public EventStoreSnapshotWriter(IEventStore eventStore, ICartRepository repo)
        {
            _eventStore = eventStore;
            _repo = repo;
        }

        public async Task WriteAsync(long cartId, CancellationToken ct = default)
        {
            // Serialize a compact read model snapshot (you can enrich as needed)
            var snap = await _repo.GetSnapshotAsync(cartId, ct);
            var snapshotJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                Cart = snap.Cart.FirstOrDefault(),
                Items = snap.Items.Select(x => new { x.ItemId, x.Quantity, x.UnitPrice }),
                Totals = snap.Totals.FirstOrDefault()
            });

            var evt = new CartSnapshotV1(cartId, snapshotJson, 0, DateTime.UtcNow, "CartService");
            await _eventStore.AppendAsync(cartId, new[] { evt }, "CartService", ct);
        }
    }
}
