using System;
using CartService.Domain.Entities;

namespace CartService.Application.Services.Abstractions
{
    public interface IIdempotentAppRequest
    {
        Task<IdempotentRequest?> FindAsync(string key, long? userId, CancellationToken ct = default);
        Task<bool> TryCreateAsync(IdempotentRequest request, CancellationToken ct = default);
        Task<IdempotentRequest?> TryAcquireLockAsync(
            string key,
            long? userId,
            DateTime utcNow,
            DateTime lockedUntil,
            DateTime expiresOn,
            string? requestHash,
            CancellationToken ct = default);
        Task PersistResponseAsync(long requestId, string responseBody, int statusCode, DateTime? expiresOn, CancellationToken ct = default);
        Task ReleaseLockAsync(long requestId, CancellationToken ct = default);
    }
}
