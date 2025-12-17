using CartService.Application.Exceptions;
using CartService.Application.Services.Abstractions;
using CartService.Domain.Entities;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CartService.Application.Services.Implementations
{
    public class IdempotencyAppService : IIdempotencyAppService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(15);
        private static readonly TimeSpan DefaultLockDuration = TimeSpan.FromSeconds(30);

        private readonly IIdempotentAppRequest _req;

        public IdempotencyAppService(IIdempotentAppRequest req) => _req = req;

        public async Task<T> ExecuteAsync<T>(
            string key,
            long? userId,
            Func<CancellationToken, Task<T>> action,
            TimeSpan? ttl = null,
            string? requestHash = null,
            CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;
            var effectiveTtl = ttl ?? DefaultTtl;
            var lockDeadline = now.Add(DefaultLockDuration);
            var expiresOn = now + effectiveTtl;

            // Return completed result if present and not expired
            var existing = await _req.FindAsync(key, userId, ct);
            if (existing is not null)
            {
                if (existing.ResponseBody is not null &&
                    (existing.ExpiresOn is null || existing.ExpiresOn > now))
                {
                    return DeserializeResult<T>(existing.ResponseBody);
                }

                EnsureHashConsistency(existing.RequestHash, requestHash);
            }

            IdempotentRequest? record = null;

            if (existing is null)
            {
                var candidate = new IdempotentRequest
                {
                    Key = key,
                    UserId = userId,
                    RequestHash = requestHash,
                    LockedUntil = lockDeadline,
                    ExpiresOn = expiresOn
                };

                if (await _req.TryCreateAsync(candidate, ct))
                {
                    record = candidate.Id != 0
                        ? candidate
                        : await _req.FindAsync(key, userId, ct);
                }
            }

            if (record is null)
            {
                var locked = await _req.TryAcquireLockAsync(key, userId, now, lockDeadline, expiresOn, requestHash, ct);
                if (locked is null)
                {
                    throw AppException.Business("request.inprogress", "The request is already being processed. Retry later.");
                }

                record = locked;
            }

            record.ExpiresOn ??= expiresOn;

            // Execute the action; clear lock on failure so retries can proceed
            try
            {
                var result = await action(ct);
                var serialized = JsonSerializer.Serialize(result, JsonOptions);

                await _req.PersistResponseAsync(record.Id, serialized, 200, record.ExpiresOn, ct);
                return result;
            }
            catch
            {
                await _req.ReleaseLockAsync(record.Id, ct);
                throw;
            }
        }

        // Optional helper for deterministic keys
        public static string ComputeHash(string route, string body)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(route + "|" + body));
            return Convert.ToBase64String(bytes);
        }

        private static T DeserializeResult<T>(string responseBody)
        {
            var restored = JsonSerializer.Deserialize<T>(responseBody, JsonOptions);
            return restored ?? throw AppException.Business("request.idempotency.deserialize",
                "Stored idempotent response cannot be deserialized to the expected type.");
        }

        private static void EnsureHashConsistency(string? storedHash, string? incomingHash)
        {
            if (storedHash is not null && incomingHash is not null &&
                !string.Equals(storedHash, incomingHash, StringComparison.Ordinal))
            {
                throw AppException.Business("request.idempotency.hash.mismatch",
                    "A different request with the same idempotency key was already processed.");
            }
        }
    }
}
