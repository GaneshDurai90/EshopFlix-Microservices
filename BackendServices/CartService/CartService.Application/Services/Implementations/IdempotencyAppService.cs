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

            // Return completed result if present and not expired
            var existing = await _req.FindAsync(key, userId, ct);
            if (existing is not null &&
                existing.ResponseBody is not null &&
                (existing.ExpiresOn is null || existing.ExpiresOn > now))
            {
                var restored = JsonSerializer.Deserialize<T>(existing.ResponseBody, JsonOptions);
                if (restored is null)
                {
                    throw AppException.Business("request.idempotency.deserialize",
                        "Stored idempotent response cannot be deserialized to the expected type.");
                }

                return restored;
            }

            // Create the record (unique on Key + UserId)
            var record = new IdempotentRequest
            {
                Key = key,
                UserId = userId,
                RequestHash = requestHash,
                LockedUntil = now.AddSeconds(30),
                ExpiresOn = ttl is null ? now.AddMinutes(15) : now + ttl.Value
            };

            var inserted = await _req.TryCreateAsync(record, ct);
            if (!inserted)
            {
                // Another request already created this key; verify hash and reuse result if available
                existing = await _req.FindAsync(key, userId, ct);
                if (existing is not null)
                {
                    // Reject different payloads under the same key if both sides provided a hash
                    if (existing.RequestHash is not null && requestHash is not null &&
                        !string.Equals(existing.RequestHash, requestHash, StringComparison.Ordinal))
                    {
                        throw AppException.Business("request.idempotency.hash.mismatch",
                            "A different request with the same idempotency key was already processed.");
                    }

                    if (existing.ResponseBody is not null)
                    {
                        var restored = JsonSerializer.Deserialize<T>(existing.ResponseBody, JsonOptions);
                        if (restored is null)
                        {
                            throw AppException.Business("request.idempotency.deserialize",
                                "Stored idempotent response cannot be deserialized to the expected type.");
                        }

                        return restored;
                    }
                }

                // Still in-flight elsewhere
                throw AppException.Business("request.inprogress", "The request is already being processed. Retry later.");
            }

            // Execute the action; leave ResponseBody null on failures so a retry can re-run
            var result = await action(ct);

            record.ResponseBody = JsonSerializer.Serialize(result);
            record.StatusCode = 200;
            record.LockedUntil = null;

            await _req.PersistResponseAsync(record, ct);
            return result;
        }

        // Optional helper for deterministic keys
        public static string ComputeHash(string route, string body)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(route + "|" + body));
            return Convert.ToBase64String(bytes);
        }
    }
}
