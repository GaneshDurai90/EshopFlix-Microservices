using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace OcelotApiGateway.Security
{
    public sealed class AuthorizationCache
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<AuthorizationCache> _logger;

        public AuthorizationCache(IDistributedCache cache, ILogger<AuthorizationCache> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public Task SetGrantAsync(string userId, string routeKey, bool isAllowed, TimeSpan ttl, CancellationToken ct = default)
        {
            var key = $"authz:{userId}:{routeKey}";
            var payload = JsonSerializer.Serialize(new { allowed = isAllowed });
            var opts = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl };
            return ExecuteAsync(() => _cache.SetStringAsync(key, payload, opts, ct));
        }

        public async Task<bool?> TryGetGrantAsync(string userId, string routeKey, CancellationToken ct = default)
        {
            var key = $"authz:{userId}:{routeKey}";
            var payload = await ExecuteAsync(() => _cache.GetStringAsync(key, ct));
            if (payload is null) return null;
            var doc = JsonSerializer.Deserialize<AuthorizationRecord>(payload);
            return doc?.allowed;
        }

        private record AuthorizationRecord(bool allowed);

        private async Task ExecuteAsync(Func<Task> callback)
        {
            try
            {
                await callback();
            }
            catch (RedisConnectionException ex)
            {
                _logger.LogWarning(ex, "Authorization cache unavailable (Redis connection), continuing without caching");
            }
            catch (RedisTimeoutException ex)
            {
                _logger.LogWarning(ex, "Authorization cache timeout (Redis), continuing without caching");
            }
            catch (Exception ex) when (IsRedisOrCacheException(ex))
            {
                _logger.LogWarning(ex, "Authorization cache unavailable, continuing without caching");
            }
        }

        private async Task<string?> ExecuteAsync(Func<Task<string?>> callback)
        {
            try
            {
                return await callback();
            }
            catch (RedisConnectionException ex)
            {
                _logger.LogWarning(ex, "Authorization cache unavailable (Redis connection), continuing without caching");
                return null;
            }
            catch (RedisTimeoutException ex)
            {
                _logger.LogWarning(ex, "Authorization cache timeout (Redis), continuing without caching");
                return null;
            }
            catch (Exception ex) when (IsRedisOrCacheException(ex))
            {
                _logger.LogWarning(ex, "Authorization cache unavailable, continuing without caching");
                return null;
            }
        }

        private static bool IsRedisOrCacheException(Exception ex)
        {
            // Catch Redis-related exceptions or general cache failures
            return ex is RedisException
                || ex is RedisCommandException
                || ex is InvalidOperationException && ex.Message.Contains("Redis", StringComparison.OrdinalIgnoreCase)
                || ex.InnerException is RedisConnectionException
                || ex.InnerException is RedisTimeoutException;
        }
    }
}