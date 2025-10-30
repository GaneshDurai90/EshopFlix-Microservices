using CartService.Application.DTOs;
using CartService.Application.Exceptions;
using System.Net;
using System.Text;
using System.Text.Json;

namespace CartService.Application.HttpClients
{
    public class CatalogServiceClient
    {
        HttpClient _client;
        public CatalogServiceClient(HttpClient client)
        {
            _client = client;
        }

        public async Task<IEnumerable<ProductDTO>> GetByIdsAsync(int[] productIds, CancellationToken cancellationToken = default)
        {
            using var content = new StringContent(JsonSerializer.Serialize(productIds), Encoding.UTF8, "application/json");
            using var response = await _client.PostAsync("catalog/getbyids", content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                throw AppException.External("CatalogService", response.StatusCode, body);
            }

            var data = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(data))
                return Enumerable.Empty<ProductDTO>();

            return JsonSerializer.Deserialize<IEnumerable<ProductDTO>>(data, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? Enumerable.Empty<ProductDTO>();
        }
    }
}
