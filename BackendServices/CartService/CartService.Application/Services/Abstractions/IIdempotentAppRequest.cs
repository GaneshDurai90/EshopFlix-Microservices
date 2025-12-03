using CartService.Domain.Entities;


namespace CartService.Application.Services.Abstractions
{
    public interface IIdempotentAppRequest
    {

        Task<IdempotentRequest?> FindAsync(string key, long? userId, CancellationToken ct = default);
        Task<bool> TryCreateAsync(IdempotentRequest request, CancellationToken ct = default);
        Task PersistResponseAsync(IdempotentRequest request, CancellationToken ct = default);

    }
}
