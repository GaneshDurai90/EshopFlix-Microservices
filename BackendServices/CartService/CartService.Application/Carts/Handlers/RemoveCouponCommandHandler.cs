using CartService.Application.CQRS;
using CartService.Application.EventSourcing;
using CartService.Application.Messaging;
using CartService.Application.Snapshots;
using CartService.Application.Repositories;
using CartService.Domain.Events;
using CartService.Application.Carts.Commands;

namespace CartService.Application.Carts.Handlers
{
    public sealed class RemoveCouponCommandHandler : ICommandHandler<RemoveCouponCommand, bool>
    {
        private readonly ICartRepository _repo;
        private readonly IEventStore _store;
        private readonly IOutboxPublisher _outbox;
        private readonly ISnapshotPolicy _snapshotPolicy;
        private readonly ISnapshotWriter _snapshotWriter;

        public RemoveCouponCommandHandler(ICartRepository repo, IEventStore store, IOutboxPublisher outbox, ISnapshotPolicy snapshotPolicy, ISnapshotWriter snapshotWriter)
        {
            _repo = repo; _store = store; _outbox = outbox; _snapshotPolicy = snapshotPolicy; _snapshotWriter = snapshotWriter;
        }

        public async Task<bool> Handle(RemoveCouponCommand command, CancellationToken ct)
        {
            await _repo.RemoveCouponAsync(command.CartId, command.Code, ct);
            var evt = new CouponRemovedV1(command.CartId, command.Code, 0, DateTime.UtcNow, "CartService");
            var version = await _store.AppendAsync(command.CartId, new[] { evt }, "CartService", ct);

            await _outbox.EnqueueAsync("Cart.CouponRemoved.v1", evt, "promotion", ct);
            if (_snapshotPolicy.ShouldSnapshot(version))
                await _snapshotWriter.WriteAsync(command.CartId, ct);

            return true;
        }
    }
}