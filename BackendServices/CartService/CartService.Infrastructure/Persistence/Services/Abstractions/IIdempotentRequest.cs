using CartService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CartService.Infrastructure.Persistence.Services.Abstractions
{
    public interface IIdempotentRequest
    {
        Task<IdempotentRequest?> FindAsync(string key, long? userId, CancellationToken ct = default);
        Task<bool> TryCreateAsync(IdempotentRequest request, CancellationToken ct = default);
        Task PersistResponseAsync(IdempotentRequest request, CancellationToken ct = default);
    }
}
