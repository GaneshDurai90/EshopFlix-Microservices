using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using StockService.Application.Repositories;
using StockService.Application.Services.Abstractions;
using StockService.Domain.Entities;
using System.Collections.Concurrent;
using System.Text.Json;

namespace StockService.Infrastructure.Services;

/// <summary>
/// Database-backed idempotency service for StockService with in-memory caching layer.
/// Uses SQL Server for durability and memory cache for performance.
/// </summary>
public sealed class DatabaseIdempotencyService : IIdempotencyService
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IIdempotentRequestStore _store;
    private readonly IMemoryCache _cache;
    private readonly ILogger<DatabaseIdempotencyService> _logger;
    private readonly TimeSpan _defaultTtl = TimeSpan.FromMinutes(15);
    private readonly TimeSpan _lockDuration = TimeSpan.FromSeconds(30);

    public DatabaseIdempotencyService(
        IIdempotentRequestStore store,
        IMemoryCache cache,
        ILogger<DatabaseIdempotencyService> logger)
    {
        _store = store;
        _cache = cache;
        _logger = logger;
    }

    public async Task<T> ExecuteAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> action,
        TimeSpan? ttl = null,
        CancellationToken ct = default)
    {
        var cacheKey = $"idem:stock:{key}";
        var gate = _locks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));
        var effectiveTtl = ttl ?? _defaultTtl;
        var now = DateTime.UtcNow;

        await gate.WaitAsync(ct);
        try
        {
            // 1. Check memory cache first (fast path)
            if (_cache.TryGetValue<T>(cacheKey, out var cachedResult) && cachedResult is not null)
            {
                _logger.LogDebug("Idempotency cache hit for key {Key}", key);
                return cachedResult;
            }

            // 2. Check database for existing response
            var existing = await _store.FindAsync(key, null, ct);
            if (existing is not null)
            {
                // Check if already processed and not expired
                if (existing.ResponseBody is not null &&
                    (existing.ExpiresOn is null || existing.ExpiresOn > now))
                {
                    _logger.LogInformation("Idempotency DB hit for key {Key}, returning stored response", key);
                    var restored = JsonSerializer.Deserialize<T>(existing.ResponseBody, JsonOptions);
                    if (restored is not null)
                    {
                        // Cache for future requests
                        CacheResult(cacheKey, restored, existing.ExpiresOn ?? now.Add(effectiveTtl));
                        return restored;
                    }
                }

                // Try to acquire lock for processing
                var locked = await _store.TryAcquireLockAsync(
                    key, null, now, now.Add(_lockDuration), now.Add(effectiveTtl), null, ct);
                
                if (locked is null)
                {
                    _logger.LogWarning("Request {Key} is being processed by another instance", key);
                    throw new InvalidOperationException("Request is already being processed. Please retry later.");
                }

                try
                {
                    return await ExecuteAndPersist(key, cacheKey, action, locked.Id, effectiveTtl, ct);
                }
                catch
                {
                    // Release lock on failure
                    await _store.ReleaseLockAsync(locked.Id, ct);
                    throw;
                }
            }

            // 3. New request - try to create record
            var newRequest = new IdempotentRequest
            {
                Key = key,
                UserId = null,
                CreatedOn = now,
                LockedUntil = now.Add(_lockDuration),
                ExpiresOn = now.Add(effectiveTtl)
            };

            if (await _store.TryCreateAsync(newRequest, ct))
            {
                try
                {
                    return await ExecuteAndPersist(key, cacheKey, action, newRequest.Id, effectiveTtl, ct);
                }
                catch
                {
                    // Release lock on failure
                    await _store.ReleaseLockAsync(newRequest.Id, ct);
                    throw;
                }
            }

            // Race condition - another request created the record, retry
            _logger.LogDebug("Race condition for key {Key}, retrying lookup", key);
            existing = await _store.FindAsync(key, null, ct);
            if (existing?.ResponseBody is not null)
            {
                var restored = JsonSerializer.Deserialize<T>(existing.ResponseBody, JsonOptions);
                if (restored is not null)
                {
                    CacheResult(cacheKey, restored, existing.ExpiresOn ?? now.Add(effectiveTtl));
                    return restored;
                }
            }

            throw new InvalidOperationException("Request is already being processed. Please retry later.");
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task<T> ExecuteAndPersist<T>(
        string key,
        string cacheKey,
        Func<CancellationToken, Task<T>> action,
        long requestId,
        TimeSpan ttl,
        CancellationToken ct)
    {
        _logger.LogDebug("Executing action for idempotency key {Key}", key);
        var result = await action(ct);

        var responseBody = JsonSerializer.Serialize(result, JsonOptions);
        var expiresOn = DateTime.UtcNow.Add(ttl);

        await _store.PersistResponseAsync(requestId, responseBody, 200, expiresOn, ct);
        CacheResult(cacheKey, result, expiresOn);

        _logger.LogDebug("Persisted response for idempotency key {Key}", key);
        return result;
    }

    private void CacheResult<T>(string cacheKey, T result, DateTimeOffset expiresAt)
    {
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpiration = expiresAt,
            Priority = CacheItemPriority.High
        };

        options.RegisterPostEvictionCallback((keyObj, _, _, _) =>
        {
            if (keyObj is string removedKey)
            {
                _locks.TryRemove(removedKey, out _);
            }
        });

        _cache.Set(cacheKey, result, options);
    }

    public async Task<(bool Found, T? Result)> TryGetAsync<T>(string key, CancellationToken ct = default)
    {
        var cacheKey = $"idem:stock:{key}";

        // Check cache first
        if (_cache.TryGetValue<T>(cacheKey, out var cachedResult) && cachedResult is not null)
        {
            return (true, cachedResult);
        }

        // Check database
        var existing = await _store.FindAsync(key, null, ct);
        if (existing?.ResponseBody is not null &&
            (existing.ExpiresOn is null || existing.ExpiresOn > DateTime.UtcNow))
        {
            var restored = JsonSerializer.Deserialize<T>(existing.ResponseBody, JsonOptions);
            if (restored is not null)
            {
                CacheResult(cacheKey, restored, existing.ExpiresOn ?? DateTime.UtcNow.AddMinutes(15));
                return (true, restored);
            }
        }

        return (false, default);
    }

    public async Task SetAsync<T>(string key, T result, TimeSpan? ttl = null, CancellationToken ct = default)
    {
        var cacheKey = $"idem:stock:{key}";
        var effectiveTtl = ttl ?? _defaultTtl;
        var now = DateTime.UtcNow;
        var expiresOn = now.Add(effectiveTtl);

        // Try to create or update in database
        var newRequest = new IdempotentRequest
        {
            Key = key,
            UserId = null,
            CreatedOn = now,
            ResponseBody = JsonSerializer.Serialize(result, JsonOptions),
            StatusCode = 200,
            ExpiresOn = expiresOn
        };

        await _store.TryCreateAsync(newRequest, ct);
        CacheResult(cacheKey, result, expiresOn);
    }
}
