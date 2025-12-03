using System.Threading;
using System.Threading.Tasks;
using CartService.Application.Services.Abstractions; // App IIdempotentRequestStore
using CartService.Domain.Entities;
using CartService.Infrastructure.Persistence.Services.Abstractions;
using InfraStore = CartService.Infrastructure.Persistence.Services.Abstractions.IIdempotentRequest;

namespace CartService.Infrastructure.Persistence.Services.Implementations
{
    // Bridges Infra EF store to the Application store abstraction
    public class IdempotentRequestAdapter : IIdempotentAppRequest
    {
        private readonly InfraStore _inner;

        public IdempotentRequestAdapter(InfraStore inner) => _inner = inner;

        public Task<IdempotentRequest?> FindAsync(string key, long? userId, CancellationToken ct = default)
            => _inner.FindAsync(key, userId, ct);

        public Task<bool> TryCreateAsync(IdempotentRequest request, CancellationToken ct = default)
            => _inner.TryCreateAsync(request, ct);

        public Task PersistResponseAsync(IdempotentRequest request, CancellationToken ct = default)
            => _inner.PersistResponseAsync(request, ct);
    }
}
