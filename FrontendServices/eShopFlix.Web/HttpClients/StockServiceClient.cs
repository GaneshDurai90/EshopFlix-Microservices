using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using eShopFlix.Web.Models.Stock;
using Microsoft.Extensions.Logging;

namespace eShopFlix.Web.HttpClients;

/// <summary>
/// HTTP client for communicating with the StockService via API Gateway.
/// </summary>
public class StockServiceClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _client;
    private readonly ILogger<StockServiceClient> _logger;

    public StockServiceClient(HttpClient client, ILogger<StockServiceClient> logger)
    {
        _client = client;
        _logger = logger;
    }

    /// <summary>
    /// Get stock availability for a product.
    /// </summary>
    public async Task<StockAvailabilityModel?> GetAvailabilityAsync(int productId, int? variationId = null, CancellationToken ct = default)
    {
        try
        {
            // StockService uses Guid for ProductId, but Catalog uses int
            // We'll need to map. For now, use productId as string in a deterministic GUID
            var productGuid = CreateDeterministicGuid(productId);
            var url = $"stock/GetAvailability/{productGuid}";
            
            if (variationId.HasValue)
            {
                var variationGuid = CreateDeterministicGuid(variationId.Value);
                url += $"?variationId={variationGuid}";
            }

            var response = await _client.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Stock availability request failed for product {ProductId}: {Status}", 
                    productId, response.StatusCode);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<StockAvailabilityModel>(JsonOptions, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stock availability for product {ProductId}", productId);
            return null;
        }
    }

    /// <summary>
    /// Check availability with specific quantity and get allocation suggestions.
    /// </summary>
    public async Task<CheckAvailabilityResultModel?> CheckAvailabilityAsync(
        int productId, int quantity, int? variationId = null, CancellationToken ct = default)
    {
        try
        {
            var productGuid = CreateDeterministicGuid(productId);
            var request = new
            {
                ProductId = productGuid,
                VariationId = variationId.HasValue ? CreateDeterministicGuid(variationId.Value) : (Guid?)null,
                Quantity = quantity
            };

            var response = await _client.PostAsJsonAsync("stock/CheckAvailability", request, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Check availability failed for product {ProductId}: {Status}", 
                    productId, response.StatusCode);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<CheckAvailabilityResultModel>(JsonOptions, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Request was cancelled (e.g., timeout) - this is expected, don't log as error
            _logger.LogDebug("Check availability cancelled for product {ProductId}", productId);
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "HTTP error checking availability for product {ProductId}", productId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking availability for product {ProductId}", productId);
            return null;
        }
    }

    /// <summary>
    /// Reserve stock for a cart.
    /// </summary>
    public async Task<ReservationResultModel?> ReserveStockAsync(
        int productId, int quantity, long cartId, long? customerId = null, int? variationId = null, CancellationToken ct = default)
    {
        try
        {
            var productGuid = CreateDeterministicGuid(productId);
            var cartGuid = CreateDeterministicGuid((int)cartId);
            
            var request = new
            {
                ProductId = productGuid,
                VariationId = variationId.HasValue ? CreateDeterministicGuid(variationId.Value) : (Guid?)null,
                Quantity = quantity,
                CartId = cartGuid,
                CustomerId = customerId.HasValue ? CreateDeterministicGuid((int)customerId.Value) : (Guid?)null,
                ReservationType = "Cart",
                TtlMinutes = 15
            };

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "stock/ReserveStock")
            {
                Content = JsonContent.Create(request)
            };
            
            // Add idempotency key with timestamp to make each reservation attempt unique
            // This prevents stale cached responses after cart is cleared
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var idempotencyPayload = $"reserve:{cartId}:{productId}:{quantity}:{timestamp}";
            var idempotencyKey = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(idempotencyPayload)));
            httpRequest.Headers.TryAddWithoutValidation("x-idempotency-key", idempotencyKey);

            var response = await _client.SendAsync(httpRequest, ct);
            var content = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Stock reservation failed for product {ProductId}: {Status} - {Content}", 
                    productId, response.StatusCode, content);
                
                // Try to parse error response
                var errorResult = JsonSerializer.Deserialize<ReservationResultModel>(content, JsonOptions);
                return errorResult ?? new ReservationResultModel { Success = false, Message = "Reservation failed" };
            }

            return JsonSerializer.Deserialize<ReservationResultModel>(content, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reserving stock for product {ProductId}", productId);
            return new ReservationResultModel { Success = false, Message = ex.Message };
        }
    }

    /// <summary>
    /// Release all reservations for a cart.
    /// </summary>
    public async Task<bool> ReleaseCartReservationsAsync(long cartId, CancellationToken ct = default)
    {
        try
        {
            var cartGuid = CreateDeterministicGuid((int)cartId);
            
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"stock/ReleaseCartReservations/{cartGuid}");
            var idempotencyKey = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"release-cart:{cartId}")));
            httpRequest.Headers.TryAddWithoutValidation("x-idempotency-key", idempotencyKey);

            var response = await _client.SendAsync(httpRequest, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error releasing reservations for cart {CartId}", cartId);
            return false;
        }
    }

    /// <summary>
    /// Get reservations for a cart.
    /// </summary>
    public async Task<IReadOnlyList<CartReservationModel>?> GetCartReservationsAsync(long cartId, CancellationToken ct = default)
    {
        try
        {
            var cartGuid = CreateDeterministicGuid((int)cartId);
            var response = await _client.GetAsync($"stock/GetCartReservations/{cartGuid}", ct);
            
            if (!response.IsSuccessStatusCode)
            {
                return Array.Empty<CartReservationModel>();
            }

            return await response.Content.ReadFromJsonAsync<IReadOnlyList<CartReservationModel>>(JsonOptions, ct)
                   ?? Array.Empty<CartReservationModel>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reservations for cart {CartId}", cartId);
            return Array.Empty<CartReservationModel>();
        }
    }

    /// <summary>
    /// Creates a deterministic GUID from an integer ID for compatibility with StockService.
    /// </summary>
    private static Guid CreateDeterministicGuid(int id)
    {
        // Create a deterministic GUID based on namespace + id
        var bytes = new byte[16];
        BitConverter.GetBytes(id).CopyTo(bytes, 0);
        // Add a namespace marker to avoid collisions
        bytes[4] = 0xE5; // eShop marker
        bytes[5] = 0x0F; // Flix marker
        return new Guid(bytes);
    }
}
