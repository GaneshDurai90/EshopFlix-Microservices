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
        private readonly CartServiceDbContext _db;
        public IdempotencyService(CartServiceDbContext db) => _db = db;

        public Task<IdempotentRequest?> FindAsync(string key, long? userId, CancellationToken ct) =>
            _db.IdempotentRequests.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Key == key && x.UserId == userId, ct);

        public async Task<bool> TryCreateAsync(IdempotentRequest request, CancellationToken ct)
        {
            _db.IdempotentRequests.Add(request);
            try
            {
                await _db.SaveChangesAsync(ct);
                return true;
            }
            catch (DbUpdateException ex) when (ex.GetBaseException() is SqlException se && (se.Number == 2601 || se.Number == 2627))
            {
                return false;
            }
        }

        public async Task PersistResponseAsync(IdempotentRequest request, CancellationToken ct)
        {
            // Attach then update only needed fields for safety
            _db.IdempotentRequests.Attach(request);
            _db.Entry(request).Property(p => p.ResponseBody).IsModified = true;
            _db.Entry(request).Property(p => p.StatusCode).IsModified = true;
            _db.Entry(request).Property(p => p.LockedUntil).IsModified = true;
            await _db.SaveChangesAsync(ct);
        }
    }
}

