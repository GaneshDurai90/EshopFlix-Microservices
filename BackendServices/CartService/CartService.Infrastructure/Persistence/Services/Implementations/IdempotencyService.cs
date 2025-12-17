using System;
using System.Linq;
using CartService.Application.Exceptions;
using CartService.Application.Services.Abstractions;
using CartService.Domain.Entities;
using CartService.Infrastructure.Persistence.Services.Abstractions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;


namespace CartService.Infrastructure.Persistence.Services.Implementations
{
    public class IdempotencyService : IIdempotentRequest
    {
        private readonly IDbContextFactory<CartServiceDbContext> _dbFactory;
        public IdempotencyService(IDbContextFactory<CartServiceDbContext> dbFactory) => _dbFactory = dbFactory;

        public async Task<IdempotentRequest?> FindAsync(string key, long? userId, CancellationToken ct)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            return await db.IdempotentRequests.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Key == key && x.UserId == userId, ct);
        }

        public async Task<bool> TryCreateAsync(IdempotentRequest request, CancellationToken ct)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            db.IdempotentRequests.Add(request);
            try
            {
                await db.SaveChangesAsync(ct);
                return true;
            }
            catch (DbUpdateException ex) when (ex.GetBaseException() is SqlException se && (se.Number == 2601 || se.Number == 2627))
            {
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
            CancellationToken ct)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var query = db.IdempotentRequests
                .Where(x => x.Key == key && x.UserId == userId && (x.LockedUntil == null || x.LockedUntil <= utcNow));

            var affected = requestHash is null
                ? await query.ExecuteUpdateAsync(setters => setters
                    .SetProperty(p => p.LockedUntil, lockedUntil)
                    .SetProperty(p => p.ExpiresOn, expiresOn), ct)
                : await query.ExecuteUpdateAsync(setters => setters
                    .SetProperty(p => p.LockedUntil, lockedUntil)
                    .SetProperty(p => p.ExpiresOn, expiresOn)
                    .SetProperty(p => p.RequestHash, requestHash), ct);

            if (affected == 0)
            {
                return null;
            }

            return await db.IdempotentRequests.AsNoTracking()
                .FirstAsync(x => x.Key == key && x.UserId == userId, ct);
        }

        public async Task PersistResponseAsync(long requestId, string responseBody, int statusCode, DateTime? expiresOn, CancellationToken ct)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var affected = await db.IdempotentRequests
                .Where(x => x.Id == requestId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(p => p.ResponseBody, responseBody)
                    .SetProperty(p => p.StatusCode, statusCode)
                    .SetProperty(p => p.LockedUntil, (DateTime?)null)
                    .SetProperty(p => p.ExpiresOn, expiresOn), ct);

            if (affected == 0)
            {
                throw AppException.Business("request.idempotency.missing", "Idempotent record could not be located for update.");
            }
        }

        public async Task ReleaseLockAsync(long requestId, CancellationToken ct)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var affected = await db.IdempotentRequests
                .Where(x => x.Id == requestId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(p => p.LockedUntil, (DateTime?)null), ct);

            if (affected == 0)
            {
                throw AppException.Business("request.idempotency.missing", "Idempotent record could not be located for update.");
            }
        }
    }
}

