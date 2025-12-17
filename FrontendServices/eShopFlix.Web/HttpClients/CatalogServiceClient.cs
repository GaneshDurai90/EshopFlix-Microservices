using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using eShopFlix.Web.Models;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace eShopFlix.Web.HttpClients
{
    public class CatalogServiceClient
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private const string PlaceholderImage = "/images/placeholders/product.svg";

        private readonly HttpClient _client;
        private readonly ILogger<CatalogServiceClient> _logger;
        private readonly string? _imageBaseUrl;

        public CatalogServiceClient(HttpClient client, IConfiguration configuration, ILogger<CatalogServiceClient> logger)
        {
            _client = client;
            _logger = logger;
            _imageBaseUrl = configuration["Catalog:ImageBaseUrl"];
        }

        public async Task<IReadOnlyList<ProductSummaryModel>> GetProductSummariesAsync(CancellationToken ct = default)
        {
            var summaries = await SendGatewayGetAsync<IReadOnlyList<ProductSummaryModel>>("catalog/getall", null, ct)
                            ?? Array.Empty<ProductSummaryModel>();

            var ids = summaries.Select(p => p.ProductId).Distinct().ToArray();
            var details = ids.Length == 0
                ? Array.Empty<ProductModel>()
                : await GetProductsByIdsAsync(ids, ct);

            var detailLookup = details.ToDictionary(d => d.ProductId, d => d);

            foreach (var summary in summaries)
            {
                if (detailLookup.TryGetValue(summary.ProductId, out var detail))
                {
                    summary.DefaultPrice = detail.UnitPrice;
                    summary.Description = detail.ShortDescription ?? detail.LongDescription ?? summary.Description;
                    summary.ImageUrl = NormalizeImageUrl(detail.PrimaryImageUrl ?? detail.ImageUrl ?? summary.PrimaryImageUrl);
                }
                else
                {
                    summary.ImageUrl = NormalizeImageUrl(summary.PrimaryImageUrl);
                }
            }

            return summaries.ToList();
        }

        public async Task<ProductDetailModel?> GetProductDetailAsync(int productId, CancellationToken ct = default)
        {
            var product = await SendGatewayGetAsync<ProductDetailModel>($"catalog/getbyid/{productId}", null, ct);
            if (product is null)
            {
                return null;
            }

            product.PrimaryImageUrl = NormalizeImageUrl(product.PrimaryImageUrl);
            return product;
        }

        public async Task<IReadOnlyList<ProductVariantModel>> GetProductVariantsAsync(int productId, CancellationToken ct = default)
        {
            var variants = await SendGatewayGetAsync<IReadOnlyList<ProductVariantModel>>($"products/{productId}/variants", null, ct);
            return variants?.Where(v => v.UnitPrice > 0).ToArray() ?? Array.Empty<ProductVariantModel>();
        }

        public async Task<IReadOnlyList<PriceHistoryEntryModel>> GetPriceHistoryAsync(int productId, int take = 10, CancellationToken ct = default)
        {
            var query = new Dictionary<string, string?>
            {
                ["ProductId"] = productId.ToString(),
                ["PageSize"] = take.ToString()
            };

            var history = await SendGatewayGetAsync<PagedResponseModel<PriceHistoryEntryModel>>("pricehistory/gethistory", query, ct);
            return history?.Items ?? Array.Empty<PriceHistoryEntryModel>();
        }

        public async Task<IReadOnlyList<ProductReviewModel>> GetProductReviewsAsync(int productId, int take = 10, CancellationToken ct = default)
        {
            var query = new Dictionary<string, string?>
            {
                ["isPublished"] = bool.TrueString,
                ["pageSize"] = take.ToString()
            };

            var reviews = await SendGatewayGetAsync<PagedResponseModel<ProductReviewModel>>($"products/{productId}/reviews", query, ct);
            return reviews?.Items ?? Array.Empty<ProductReviewModel>();
        }

        public async Task<IReadOnlyList<PromotionSummaryModel>> GetActivePromotionsAsync(int take = 3, CancellationToken ct = default)
        {
            var query = new Dictionary<string, string?>
            {
                ["IsActive"] = bool.TrueString,
                ["PageSize"] = take.ToString()
            };

            var promotions = await SendGatewayGetAsync<PagedResponseModel<PromotionSummaryModel>>("promotion/search", query, ct);
            return promotions?.Items ?? Array.Empty<PromotionSummaryModel>();
        }

        public async Task<IReadOnlyList<ProductModel>> GetProductsByIdsAsync(IEnumerable<int> ids, CancellationToken ct = default)
        {
            var distinctIds = ids?.Distinct().ToArray() ?? Array.Empty<int>();
            if (distinctIds.Length == 0)
            {
                return Array.Empty<ProductModel>();
            }

            var payload = new { ids = distinctIds };
            using var response = await _client.PostAsJsonAsync("catalog/getbyids", payload, JsonOptions, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Catalog request catalog/getbyids failed with {StatusCode}", response.StatusCode);
                return Array.Empty<ProductModel>();
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            var products = await JsonSerializer.DeserializeAsync<IReadOnlyList<ProductModel>>(stream, JsonOptions, ct);
            if (products == null)
            {
                return Array.Empty<ProductModel>();
            }

            foreach (var product in products)
            {
                var preferredImage = string.IsNullOrWhiteSpace(product.PrimaryImageUrl)
                    ? product.ImageUrl
                    : product.PrimaryImageUrl;
                product.ImageUrl = NormalizeImageUrl(preferredImage);
            }

            return products;
        }

        private async Task<T?> SendGatewayGetAsync<T>(string relativePath, IDictionary<string, string?>? query, CancellationToken ct)
        {
            var requestPath = query is null ? relativePath : QueryHelpers.AddQueryString(relativePath, query);

            using var response = await _client.GetAsync(requestPath, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Catalog gateway request {Path} failed: {StatusCode}", relativePath, response.StatusCode);
                return default;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, ct);
        }

        private string NormalizeImageUrl(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return PlaceholderImage;
            }

            if (Uri.TryCreate(path, UriKind.Absolute, out var absolute))
            {
                return absolute.ToString();
            }

            if (!string.IsNullOrWhiteSpace(_imageBaseUrl))
            {
                return _imageBaseUrl!.TrimEnd('/') + "/" + path.TrimStart('/');
            }

            return path;
        }
    }
}
