using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;

public sealed class JwksCacheService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IDistributedCache _cache;
    private readonly ILogger<JwksCacheService> _logger;
    private const string CacheKey = "jwks:issuer";

    public JwksCacheService(IHttpClientFactory httpClientFactory, IDistributedCache cache, ILogger<JwksCacheService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _logger = logger;
    }

    public async Task<JsonDocument?> GetKeysAsync(string jwksUrl, CancellationToken ct)
    {
        var cached = await _cache.GetStringAsync(CacheKey, ct);
        if (cached is not null)
        {
            _logger.LogInformation("JWKS served from cache (key={CacheKey}).", CacheKey);
            return JsonDocument.Parse(cached);
        }

        try
        {
            var client = _httpClientFactory.CreateClient("jwks");
            var doc = await client.GetFromJsonAsync<JsonDocument>(jwksUrl, ct);
            if (doc is not null)
            {
                var payload = doc.RootElement.GetRawText();
                await _cache.SetStringAsync(
                    CacheKey,
                    payload,
                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30) },
                    ct);
                _logger.LogInformation("JWKS fetched and cached (ttl=30m, key={CacheKey}).", CacheKey);
                return doc;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "JWKS fetch failed for {JwksUrl}, falling back to cached keys if present.", jwksUrl);
            if (cached is not null)
                return JsonDocument.Parse(cached);
        }
        return null;
    }
}