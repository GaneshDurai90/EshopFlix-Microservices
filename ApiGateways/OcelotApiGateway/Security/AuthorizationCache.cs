using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace OcelotApiGateway.Security
{
    public sealed class AuthorizationCache
    {
        private readonly IDistributedCache _cache;
        public AuthorizationCache(IDistributedCache cache) => _cache = cache;

        public Task SetGrantAsync(string userId, string routeKey, bool isAllowed, TimeSpan ttl, CancellationToken ct = default)
        {
            var key = $"authz:{userId}:{routeKey}";
            var payload = JsonSerializer.Serialize(new { allowed = isAllowed });
            var opts = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl };
            return _cache.SetStringAsync(key, payload, opts, ct);
        }

        public async Task<bool?> TryGetGrantAsync(string userId, string routeKey, CancellationToken ct = default)
        {
            var key = $"authz:{userId}:{routeKey}";
            var payload = await _cache.GetStringAsync(key, ct);
            if (payload is null) return null;
            var doc = JsonSerializer.Deserialize<AuthorizationRecord>(payload);
            return doc?.allowed;
        }

        private record AuthorizationRecord(bool allowed);
    }
}