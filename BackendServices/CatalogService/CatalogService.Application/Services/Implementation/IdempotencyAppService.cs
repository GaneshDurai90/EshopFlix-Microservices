using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CatalogService.Application.Exceptions;
using CatalogService.Application.Services.Abstractions;
using CatalogService.Domain.Entities;

namespace CatalogService.Application.Services.Implementation
{
    public sealed class IdempotencyAppService : IIdempotencyAppService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly IIdempotentAppRequest _store;

        public IdempotencyAppService(IIdempotentAppRequest store)
        {
            _store = store;
        }

        public async Task<T> ExecuteAsync<T>(
            string key,
            long? userId,
            Func<CancellationToken, Task<T>> action,
            TimeSpan? ttl = null,
            string? requestHash = null,
            CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;
            var existing = await _store.FindAsync(key, userId, ct);
            if (existing is not null &&
                existing.ResponseBody is not null &&
                (existing.ExpiresOn is null || existing.ExpiresOn > now))
            {
                var restored = JsonSerializer.Deserialize<T>(existing.ResponseBody, JsonOptions);
                if (restored is null)
                {
                    throw AppException.Business("request.idempotency.deserialize", "Stored idempotent response cannot be deserialized to the expected type.");
                }

                return restored;
            }

            var record = new IdempotentRequest
            {
                Key = key,
                UserId = userId,
                RequestHash = requestHash,
                LockedUntil = now.AddSeconds(30),
                ExpiresOn = ttl is null ? now.AddMinutes(15) : now + ttl.Value,
                CreatedOn = now
            };

            var inserted = await _store.TryCreateAsync(record, ct);
            if (!inserted)
            {
                existing = await _store.FindAsync(key, userId, ct);
                if (existing is not null)
                {
                    if (existing.RequestHash is not null && requestHash is not null &&
                        !string.Equals(existing.RequestHash, requestHash, StringComparison.Ordinal))
                    {
                        throw AppException.Business("request.idempotency.hash.mismatch", "A different request with the same idempotency key was already processed.");
                    }

                    if (existing.ResponseBody is not null)
                    {
                        var restored = JsonSerializer.Deserialize<T>(existing.ResponseBody, JsonOptions);
                        if (restored is null)
                        {
                            throw AppException.Business("request.idempotency.deserialize", "Stored idempotent response cannot be deserialized to the expected type.");
                        }

                        return restored;
                    }
                }

                throw AppException.Business("request.inprogress", "The request is already being processed. Retry later.");
            }

            var result = await action(ct);

            record.ResponseBody = JsonSerializer.Serialize(result, JsonOptions);
            record.StatusCode = 200;
            record.LockedUntil = null;

            await _store.PersistResponseAsync(record, ct);
            return result;
        }

        public static string ComputeDeterministicKey(string route, string body)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(route + "|" + body));
            return Convert.ToBase64String(bytes);
        }
    }
}
