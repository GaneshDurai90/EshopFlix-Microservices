using CartService.Application.CQRS;
using CartService.Application.EventSourcing;
using CartService.Application.Messaging;
using CartService.Application.Snapshots;
using CartService.Application.Repositories;
using CartService.Domain.Events;
using CartService.Application.Carts.Commands;

namespace CartService.Application.Carts.Handlers
{
    public sealed class MakeInActiveCommandHandler : ICommandHandler<MakeInActiveCommand, bool>
    {
        private readonly ICartRepository _repo;
        private readonly IEventStore _store;
        private readonly IOutboxPublisher _outbox;
        private readonly ISnapshotPolicy _snapshotPolicy;
        private readonly ISnapshotWriter _snapshotWriter;

        public MakeInActiveCommandHandler(ICartRepository repo, IEventStore store, IOutboxPublisher outbox, ISnapshotPolicy snapshotPolicy, ISnapshotWriter snapshotWriter)
        {
            _repo = repo; _store = store; _outbox = outbox; _snapshotPolicy = snapshotPolicy; _snapshotWriter = snapshotWriter;
        }

        public async Task<bool> Handle(MakeInActiveCommand command, CancellationToken ct)
        {
            var ok = await _repo.MakeInActive(command.CartId);
            if (!ok) return false;

            var evt = new CartDeactivatedV1(command.CartId, 0, DateTime.UtcNow, "CartService");
            var version = await _store.AppendAsync(command.CartId, new[] { evt }, "CartService", ct);

            await _outbox.EnqueueAsync("Cart.Deactivated.v1", evt, "orders", ct);
            if (_snapshotPolicy.ShouldSnapshot(version))
                await _snapshotWriter.WriteAsync(command.CartId, ct);

            return true;
        }
    }
}