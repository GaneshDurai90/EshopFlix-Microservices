using Microsoft.Extensions.Caching.Distributed;
using System.Security.Claims;
using System.Text.Json;

public sealed class ClaimsCacheService
{
    private readonly IDistributedCache _cache;
    public ClaimsCacheService(IDistributedCache cache) => _cache = cache;

    public async Task CacheClaimsAsync(string userId, IEnumerable<Claim> claims, CancellationToken ct)
    {
        var payload = JsonSerializer.Serialize(claims.Select(c => new { c.Type, c.Value }));
        await _cache.SetStringAsync($"claims:{userId}", payload,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15) }, ct);
    }

    public async Task<Claim[]?> GetClaimsAsync(string userId, CancellationToken ct)
    {
        var payload = await _cache.GetStringAsync($"claims:{userId}", ct);
        if (payload is null) return null;
        var items = JsonSerializer.Deserialize<List<(string Type, string Value)>>(payload);
        return items?.Select(i => new Claim(i.Type, i.Value)).ToArray();
    }
}