using CartService.Application.DTOs;
using CartService.Application.Exceptions;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace CartService.Application.HttpClients
{
    public class CatalogServiceClient
    {
        private readonly HttpClient _client;
        private readonly ILogger<CatalogServiceClient> _logger;

        public CatalogServiceClient(HttpClient client, ILogger<CatalogServiceClient> logger)
        {
            _client = client;
            _logger = logger;
        }

        public async Task<IEnumerable<ProductDTO>> GetByIdsAsync(int[] productIds, CancellationToken cancellationToken = default)
        {
            if (productIds == null || productIds.Length == 0)
            {
                return Enumerable.Empty<ProductDTO>();
            }

            try
            {
                // Use lowercase 'ids' to match common JSON conventions and the request contract
                var payload = new { ids = productIds };
                using var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                
                // The endpoint path should match the CatalogController route: api/Catalog/GetByIds
                using var response = await _client.PostAsync("Catalog/GetByIds", content, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogWarning("CatalogService GetByIds failed with {StatusCode}: {Body}", response.StatusCode, body);
                    
                    // Return empty instead of throwing - let the caller handle gracefully
                    return Enumerable.Empty<ProductDTO>();
                }

                var data = await response.Content.ReadAsStringAsync(cancellationToken);
                if (string.IsNullOrWhiteSpace(data))
                    return Enumerable.Empty<ProductDTO>();

                return JsonSerializer.Deserialize<IEnumerable<ProductDTO>>(data, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? Enumerable.Empty<ProductDTO>();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "CatalogService is unavailable");
                return Enumerable.Empty<ProductDTO>();
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogWarning(ex, "CatalogService request timed out");
                return Enumerable.Empty<ProductDTO>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error calling CatalogService GetByIds");
                return Enumerable.Empty<ProductDTO>();
            }
        }
    }
}
