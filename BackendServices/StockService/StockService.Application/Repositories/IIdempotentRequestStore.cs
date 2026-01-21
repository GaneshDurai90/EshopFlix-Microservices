using StockService.Domain.Entities;

namespace StockService.Application.Repositories;

/// <summary>
/// Repository interface for idempotent request storage.
/// </summary>
public interface IIdempotentRequestStore
{
    /// <summary>
    /// Finds an existing idempotent request by key and optional user ID.
    /// </summary>
    Task<IdempotentRequest?> FindAsync(string key, long? userId, CancellationToken ct = default);

    /// <summary>
    /// Tries to create a new idempotent request record.
    /// Returns false if a record with the same key already exists.
    /// </summary>
    Task<bool> TryCreateAsync(IdempotentRequest request, CancellationToken ct = default);

    /// <summary>
    /// Tries to acquire a lock on an existing request for processing.
    /// Returns the request if lock was acquired, null otherwise.
    /// </summary>
    Task<IdempotentRequest?> TryAcquireLockAsync(
        string key,
        long? userId,
        DateTime utcNow,
        DateTime lockedUntil,
        DateTime expiresOn,
        string? requestHash,
        CancellationToken ct = default);

    /// <summary>
    /// Persists the response for a processed request.
    /// </summary>
    Task PersistResponseAsync(
        long requestId,
        string responseBody,
        int statusCode,
        DateTime? expiresOn,
        CancellationToken ct = default);

    /// <summary>
    /// Releases the lock on a request (e.g., if processing failed).
    /// </summary>
    Task ReleaseLockAsync(long requestId, CancellationToken ct = default);
}
