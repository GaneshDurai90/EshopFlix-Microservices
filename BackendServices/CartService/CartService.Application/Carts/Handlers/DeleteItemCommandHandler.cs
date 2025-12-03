using CartService.Application.CQRS;
using CartService.Application.EventSourcing;
using CartService.Application.Messaging;
using CartService.Application.Snapshots;
using CartService.Application.Repositories;
using CartService.Domain.Events;
using CartService.Application.Carts.Commands;

namespace CartService.Application.Carts.Handlers
{
    public sealed class DeleteItemCommandHandler : ICommandHandler<DeleteItemCommand, int>
    {
        private readonly ICartRepository _repo;
        private readonly IEventStore _store;
        private readonly IOutboxPublisher _outbox;
        private readonly ISnapshotPolicy _snapshotPolicy;
        private readonly ISnapshotWriter _snapshotWriter;

        public DeleteItemCommandHandler(ICartRepository repo, IEventStore store, IOutboxPublisher outbox, ISnapshotPolicy snapshotPolicy, ISnapshotWriter snapshotWriter)
        {
            _repo = repo; _store = store; _outbox = outbox; _snapshotPolicy = snapshotPolicy; _snapshotWriter = snapshotWriter;
        }

        public async Task<int> Handle(DeleteItemCommand command, CancellationToken ct)
        {
            var affected = await _repo.DeleteItem(command.CartId, command.ItemId);
            var evt = new ItemRemovedV1(command.CartId, command.ItemId, 0, DateTime.UtcNow, "CartService");
            var version = await _store.AppendAsync(command.CartId, new[] { evt }, "CartService", ct);

            await _outbox.EnqueueAsync("Cart.ItemRemoved.v1", evt, "inventory", ct);
            if (_snapshotPolicy.ShouldSnapshot(version))
                await _snapshotWriter.WriteAsync(command.CartId, ct);

            return affected;
        }
    }
}