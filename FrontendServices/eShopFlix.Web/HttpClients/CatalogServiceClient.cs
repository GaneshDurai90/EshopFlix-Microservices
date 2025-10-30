using eShopFlix.Web.Models;
using System.Text.Json;

namespace eShopFlix.Web.HttpClients
{
    public class CatalogServiceClient
    {
        private readonly HttpClient _client;
        private readonly string? _imageBaseUrl;
        public CatalogServiceClient(HttpClient client, IConfiguration configuration)
        {
            _client = client;
            _imageBaseUrl = configuration["Catalog:ImageBaseUrl"]; // e.g., https://localhost:7159
        }

        public async Task<IEnumerable<ProductModel>> GetProducts()
        {
            using var response = await _client.GetAsync("catalog/getall");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var products = JsonSerializer.Deserialize<IEnumerable<ProductModel>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? Enumerable.Empty<ProductModel>();

                if (!string.IsNullOrWhiteSpace(_imageBaseUrl))
                {
                    foreach (var p in products)
                    {
                        if (!string.IsNullOrWhiteSpace(p.ImageUrl) && p.ImageUrl.StartsWith("/"))
                        {
                            p.ImageUrl = _imageBaseUrl.TrimEnd('/') + p.ImageUrl;
                        }
                    }
                }
                return products;
            }
            return Enumerable.Empty<ProductModel>();
        }
    }
}
