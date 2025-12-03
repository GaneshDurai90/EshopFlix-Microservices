using System;
using System.Threading;
using System.Threading.Tasks;

namespace CartService.Infrastructure.Persistence.Services.Abstractions
{
    public interface IIdempotencyService
    {
        Task<T> ExecuteAsync<T>(
            string key,
            long? userId,
            Func<CancellationToken, Task<T>> action,
            TimeSpan ttl,
            string? requestHash = null,
            CancellationToken ct = default);
    }
}
