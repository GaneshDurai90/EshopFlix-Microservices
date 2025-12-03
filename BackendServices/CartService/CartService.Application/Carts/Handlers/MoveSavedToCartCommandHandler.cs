using CartService.Application.CQRS;
using CartService.Application.EventSourcing;
using CartService.Application.Snapshots;
using CartService.Application.Repositories;
using CartService.Domain.Events;
using CartService.Application.Carts.Commands;

namespace CartService.Application.Carts.Handlers
{
    public sealed class MoveSavedToCartCommandHandler : ICommandHandler<MoveSavedToCartCommand, bool>
    {
        private readonly ICartRepository _repo;
        private readonly IEventStore _store;
        private readonly ISnapshotPolicy _snapshotPolicy;
        private readonly ISnapshotWriter _snapshotWriter;

        public MoveSavedToCartCommandHandler(ICartRepository repo, IEventStore store, ISnapshotPolicy snapshotPolicy, ISnapshotWriter snapshotWriter)
        {
            _repo = repo; _store = store; _snapshotPolicy = snapshotPolicy; _snapshotWriter = snapshotWriter;
        }

        public async Task<bool> Handle(MoveSavedToCartCommand command, CancellationToken ct)
        {
            await _repo.MoveSavedToCartAsync(command.SavedItemId, ct);
            var evt = new SavedItemMovedToCartV1(0, command.SavedItemId, 0, DateTime.UtcNow, "CartService"); // CartId will be resolved via repo inside projection; keep 0 if unknown here
            // Optional: derive cartId by querying SavedItem first; kept simple here.

            // No append when cartId unknown. If required, resolve cartId via a repository call above, then append.
            return true;
        }
    }
}