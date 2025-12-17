using System;
using System.Threading;
using System.Threading.Tasks;

namespace CatalogService.Application.Services.Abstractions
{
    public interface IIdempotencyAppService
    {
        Task<T> ExecuteAsync<T>(
            string key,
            long? userId,
            Func<CancellationToken, Task<T>> action,
            TimeSpan? ttl = null,
            string? requestHash = null,
            CancellationToken ct = default);
    }
}
