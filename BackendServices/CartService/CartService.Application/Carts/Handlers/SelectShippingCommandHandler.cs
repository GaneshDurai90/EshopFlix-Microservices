using CartService.Application.CQRS;
using CartService.Application.EventSourcing;
using CartService.Application.Messaging;
using CartService.Application.Snapshots;
using CartService.Application.Repositories;
using CartService.Domain.Events;
using CartService.Application.Carts.Commands;

namespace CartService.Application.Carts.Handlers
{
    public sealed class SelectShippingCommandHandler : ICommandHandler<SelectShippingCommand, bool>
    {
        private readonly ICartRepository _repo;
        private readonly IEventStore _store;
        private readonly IOutboxPublisher _outbox;
        private readonly ISnapshotPolicy _snapshotPolicy;
        private readonly ISnapshotWriter _snapshotWriter;

        public SelectShippingCommandHandler(ICartRepository repo, IEventStore store, IOutboxPublisher outbox, ISnapshotPolicy snapshotPolicy, ISnapshotWriter snapshotWriter)
        {
            _repo = repo; _store = store; _outbox = outbox; _snapshotPolicy = snapshotPolicy; _snapshotWriter = snapshotWriter;
        }

        public async Task<bool> Handle(SelectShippingCommand command, CancellationToken ct)
        {
            await _repo.SelectShippingAsync(command.CartId, command.Carrier, command.MethodCode, command.MethodName, command.Cost, command.EstimatedDays, command.AddressSnapshotJson, ct);
            var evt = new ShippingSelectedV1(command.CartId, command.Carrier, command.MethodCode, command.MethodName, command.Cost, command.EstimatedDays, command.AddressSnapshotJson, 0, DateTime.UtcNow, "CartService");
            var version = await _store.AppendAsync(command.CartId, new[] { evt }, "CartService", ct);

            await _outbox.EnqueueAsync("Cart.ShippingSelected.v1", evt, "shipping", ct);
            if (_snapshotPolicy.ShouldSnapshot(version))
                await _snapshotWriter.WriteAsync(command.CartId, ct);

            return true;
        }
    }
}