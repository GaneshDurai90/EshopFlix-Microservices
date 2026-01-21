using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StockService.Application.Repositories;
using StockService.Domain.Entities;
using StockService.Infrastructure.Persistence;

namespace StockService.Infrastructure.Repositories;

/// <summary>
/// Database-backed implementation of idempotent request storage.
/// </summary>
public class IdempotentRequestStore : IIdempotentRequestStore
{
    private readonly StockServiceDbContext _db;
    private readonly ILogger<IdempotentRequestStore> _logger;

    public IdempotentRequestStore(StockServiceDbContext db, ILogger<IdempotentRequestStore> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IdempotentRequest?> FindAsync(string key, long? userId, CancellationToken ct = default)
    {
        return await _db.IdempotentRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Key == key && x.UserId == userId, ct);
    }

    public async Task<bool> TryCreateAsync(IdempotentRequest request, CancellationToken ct = default)
    {
        _db.IdempotentRequests.Add(request);
        try
        {
            await _db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException ex) when (ex.GetBaseException() is SqlException sql && 
                                           (sql.Number == 2601 || sql.Number == 2627)) // Unique constraint violation
        {
            _logger.LogDebug("Idempotent request already exists for key {Key}", request.Key);
            return false;
        }
    }

    public async Task<IdempotentRequest?> TryAcquireLockAsync(
        string key,
        long? userId,
        DateTime utcNow,
        DateTime lockedUntil,
        DateTime expiresOn,
        string? requestHash,
        CancellationToken ct = default)
    {
        // Try to update an existing unlocked record
        var query = _db.IdempotentRequests
            .Where(x => x.Key == key && x.UserId == userId && 
                       (x.LockedUntil == null || x.LockedUntil <= utcNow));

        int affected;
        if (requestHash is null)
        {
            affected = await query.ExecuteUpdateAsync(setters => setters
                .SetProperty(p => p.LockedUntil, lockedUntil)
                .SetProperty(p => p.ExpiresOn, expiresOn), ct);
        }
        else
        {
            affected = await query.ExecuteUpdateAsync(setters => setters
                .SetProperty(p => p.LockedUntil, lockedUntil)
                .SetProperty(p => p.ExpiresOn, expiresOn)
                .SetProperty(p => p.RequestHash, requestHash), ct);
        }

        if (affected == 0)
        {
            return null;
        }

        return await _db.IdempotentRequests
            .AsNoTracking()
            .FirstAsync(x => x.Key == key && x.UserId == userId, ct);
    }

    public async Task PersistResponseAsync(
        long requestId,
        string responseBody,
        int statusCode,
        DateTime? expiresOn,
        CancellationToken ct = default)
    {
        var affected = await _db.IdempotentRequests
            .Where(x => x.Id == requestId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(p => p.ResponseBody, responseBody)
                .SetProperty(p => p.StatusCode, statusCode)
                .SetProperty(p => p.LockedUntil, (DateTime?)null)
                .SetProperty(p => p.ExpiresOn, expiresOn), ct);

        if (affected == 0)
        {
            _logger.LogWarning("Failed to persist response for idempotent request {RequestId}", requestId);
        }
    }

    public async Task ReleaseLockAsync(long requestId, CancellationToken ct = default)
    {
        await _db.IdempotentRequests
            .Where(x => x.Id == requestId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(p => p.LockedUntil, (DateTime?)null), ct);
    }
}
