using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CartService.Application.Services.Abstractions
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
