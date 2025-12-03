using CartService.Application.CQRS;
using CartService.Application.EventSourcing;
using CartService.Application.Snapshots;
using CartService.Application.Repositories;
using CartService.Domain.Events;
using CartService.Application.Carts.Commands;

namespace CartService.Application.Carts.Handlers
{
    public sealed class RecalculateTotalsCommandHandler : ICommandHandler<RecalculateTotalsCommand, bool>
    {
        private readonly ICartRepository _repo;
        private readonly IEventStore _store;
        private readonly ISnapshotPolicy _snapshotPolicy;
        private readonly ISnapshotWriter _snapshotWriter;

        public RecalculateTotalsCommandHandler(ICartRepository repo, IEventStore store, ISnapshotPolicy snapshotPolicy, ISnapshotWriter snapshotWriter)
        {
            _repo = repo; _store = store; _snapshotPolicy = snapshotPolicy; _snapshotWriter = snapshotWriter;
        }

        public async Task<bool> Handle(RecalculateTotalsCommand command, CancellationToken ct)
        {
            await _repo.RecalculateTotalsAsync(command.CartId, ct);
            var evt = new TotalsRecalculatedV1(command.CartId, 0, DateTime.UtcNow, "CartService");
            var version = await _store.AppendAsync(command.CartId, new[] { evt }, "CartService", ct);

            if (_snapshotPolicy.ShouldSnapshot(version))
                await _snapshotWriter.WriteAsync(command.CartId, ct);

            return true;
        }
    }
}