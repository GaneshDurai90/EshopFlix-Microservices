using CartService.Application.Exceptions;
using CartService.Infrastructure.Persistence.Services.Abstractions;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;
using System.Text.Json;

namespace CartService.Infrastructure.Persistence.Services.Implementations
{
    /// <summary>
    /// In-memory idempotency orchestration used by API layer.
    /// Distinct from the DB-backed request store (IdempotencyService implementing IIdempotentRequest).
    /// </summary>
    public sealed class InMemoryIdempotencyCoordinator : IIdempotencyService
    {
        private sealed record CacheEntry(string? Hash, object? Result, DateTimeOffset ExpiresAt);

        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly IMemoryCache _cache;

        public InMemoryIdempotencyCoordinator(IMemoryCache cache) => _cache = cache;

        public async Task<T> ExecuteAsync<T>(
            string key,
            long? userId,
            Func<CancellationToken, Task<T>> action,
            TimeSpan ttl,
            string? requestHash = null,
            CancellationToken ct = default)
        {
            var cacheKey = $"idem:{userId}:{key}";
            var gate = _locks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));

            await gate.WaitAsync(ct);
            try
            {
                if (_cache.TryGetValue<CacheEntry>(cacheKey, out var entry) &&
                    entry.ExpiresAt > DateTimeOffset.UtcNow)
                {
                    // Prevent re-use with different payload/hash
                    if (entry.Hash != null && requestHash != null &&
                        !string.Equals(entry.Hash, requestHash, StringComparison.Ordinal))
                    {
                        throw AppException.Business("request.idempotency.hash.mismatch",
                            "A different request with the same idempotency key was already processed.");
                    }

                    if (entry.Result is T typed)
                    {
                        return typed;
                    }

                    if (entry.Result is not null)
                    {
                        var json = JsonSerializer.Serialize(entry.Result, JsonOptions);
                        var restored = JsonSerializer.Deserialize<T>(json, JsonOptions);
                        if (restored is not null)
                        {
                            return restored;
                        }
                    }
                }

                var result = await action(ct);

                var newEntry = new CacheEntry(requestHash, result, DateTimeOffset.UtcNow.Add(ttl));
                var options = new MemoryCacheEntryOptions
                {
                    AbsoluteExpiration = newEntry.ExpiresAt
                };

                // Remove the per-key semaphore when the cache entry is evicted/expired to avoid leaks
                options.RegisterPostEvictionCallback((keyObj, valueObj, reason, state) =>
                {
                    if (keyObj is string removedKey)
                    {
                        _locks.TryRemove(removedKey, out _);
                    }
                });

                _cache.Set(cacheKey, newEntry, options);

                return result;
            }
            finally
            {
                gate.Release();

                // If no cache entry exists (e.g., action failed), clean up the semaphore for this key.
                if (!_cache.TryGetValue(cacheKey, out _))
                {
                    _locks.TryRemove(cacheKey, out _);
                }
            }
        }
    }
}