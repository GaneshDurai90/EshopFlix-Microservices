using System.Text.Json;
using System.Text;
using eShopFlix.Web.Models;

namespace eShopFlix.Web.HttpClients
{
    public class CartServiceClient
    {
        private readonly HttpClient _client;
        private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

        public CartServiceClient(HttpClient client)
        {
            _client = client;
        }

        // Header helpers
        private static void AddCommonHeaders(HttpRequestMessage req, string? idempotencyPayload = null)
        {
            // Correlation
            req.Headers.TryAddWithoutValidation("x-correlation-id", Guid.NewGuid().ToString("N"));

            // Idempotency only for write operations
            if (idempotencyPayload != null)
            {
                var key = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
                    Encoding.UTF8.GetBytes(idempotencyPayload)));
                req.Headers.TryAddWithoutValidation("x-idempotency-key", key);
            }
        }

        public async Task<CartModel?> GetUserCartAsync(long UserId)
        {
            using var response = await _client.GetAsync($"cart/getusercart/{UserId}");
            if (!response.IsSuccessStatusCode) return null;
            var content = await response.Content.ReadAsStringAsync();
            return string.IsNullOrWhiteSpace(content) ? null : JsonSerializer.Deserialize<CartModel>(content, JsonOpts);
        }

        public Task<CartModel?> GetCartAsync(long CartId)
            => _client.GetFromJsonAsync<CartModel>($"cart/getcart/{CartId}");

        public Task<bool> MakeCartInActiveAsync(long CartId)
            => _client.GetFromJsonAsync<bool>($"cart/makeinactive/{CartId}");

        public async Task<CartModel?> AddToCartAsync(CartItemModel item, long UserId)
        {
            var payloadJson = JsonSerializer.Serialize(item);
            using var req = new HttpRequestMessage(HttpMethod.Post, $"cart/additem/{UserId}")
            {
                Content = new StringContent(payloadJson, Encoding.UTF8, "application/json")
            };
            AddCommonHeaders(req, $"add:{UserId}:{payloadJson}");
            using var resp = await _client.SendAsync(req);
            if (!resp.IsSuccessStatusCode) return null;
            return await resp.Content.ReadFromJsonAsync<CartModel>(JsonOpts);
        }

        public async Task<int> DeleteCartItemAsync(long CartId, int ItemId)
        {
            using var req = new HttpRequestMessage(HttpMethod.Delete, $"cart/deleteItem/{CartId}/{ItemId}");
            AddCommonHeaders(req, $"del:{CartId}:{ItemId}");
            using var resp = await _client.SendAsync(req);
            if (!resp.IsSuccessStatusCode) return 0;
            var body = await resp.Content.ReadAsStringAsync();
            return int.TryParse(body, out var v) ? v : await resp.Content.ReadFromJsonAsync<int>();
        }

        public async Task<int> UpdateQuantity(long CartId, int ItemId, int Quantity)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, $"cart/UpdateQuantity/{CartId}/{ItemId}/{Quantity}");
            AddCommonHeaders(req, $"qty:{CartId}:{ItemId}:{Quantity}");
            using var resp = await _client.SendAsync(req);
            if (!resp.IsSuccessStatusCode) return 0;
            var body = await resp.Content.ReadAsStringAsync();
            return int.TryParse(body, out var v) ? v : await resp.Content.ReadFromJsonAsync<int>();
        }

        public async Task<int> GetCartItemCount(long UserId)
        {
            // Don't make API call with invalid userId
            if (UserId <= 0) return 0;
            
            try
            {
                return await _client.GetFromJsonAsync<int>($"cart/GetCartItemCount/{UserId}");
            }
            catch
            {
                return 0;
            }
        }

        // Extended endpoints (unchanged reads)
        public Task<CartTotalsModel?> GetTotalsAsync(long cartId)
            => _client.GetFromJsonAsync<CartTotalsModel>($"cart/totals/{cartId}");

        public Task<List<CouponModel>?> GetCouponsAsync(long cartId)
            => _client.GetFromJsonAsync<List<CouponModel>>($"cart/coupons/{cartId}");

        public async Task<bool> ApplyCouponAsync(long cartId, string code, decimal amount, string? description = null)
        {
            var payload = new { CartId = cartId, Code = code, Amount = amount, Description = description };
            using var req = new HttpRequestMessage(HttpMethod.Post, "cart/applycoupon")
            {
                Content = JsonContent.Create(payload)
            };
            AddCommonHeaders(req, $"coupon:add:{cartId}:{code}:{amount}");
            var resp = await _client.SendAsync(req);
            return resp.IsSuccessStatusCode;
        }

        public async Task<bool> RemoveCouponAsync(long cartId, string code)
        {
            using var req = new HttpRequestMessage(HttpMethod.Delete, $"cart/removecoupon/{cartId}/{code}");
            AddCommonHeaders(req, $"coupon:rem:{cartId}:{code}");
            var resp = await _client.SendAsync(req);
            return resp.IsSuccessStatusCode;
        }

        public Task<List<ShipmentModel>?> GetShipmentsAsync(long cartId)
            => _client.GetFromJsonAsync<List<ShipmentModel>>($"cart/shipments/{cartId}");

        public async Task<bool> SelectShippingAsync(long cartId, ShipmentModel method, string? addressSnapshotJson = null)
        {
            var payload = new
            {
                CartId = cartId,
                method.Carrier,
                method.MethodCode,
                method.MethodName,
                method.Cost,
                method.EstimatedDays,
                AddressSnapshotJson = addressSnapshotJson
            };
            using var req = new HttpRequestMessage(HttpMethod.Post, "cart/selectshipping")
            {
                Content = JsonContent.Create(payload)
            };
            AddCommonHeaders(req, $"ship:{cartId}:{method.MethodCode}:{method.Cost}");
            var resp = await _client.SendAsync(req);
            return resp.IsSuccessStatusCode;
        }

        public Task<List<TaxLineModel>?> GetTaxesAsync(long cartId)
            => _client.GetFromJsonAsync<List<TaxLineModel>>($"cart/taxes/{cartId}");

        public async Task<bool> ClearAsync(long cartId)
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, $"cart/clear/{cartId}");
            AddCommonHeaders(req, $"clear:{cartId}");
            var resp = await _client.SendAsync(req);
            return resp.IsSuccessStatusCode;
        }

        public Task<List<SavedForLaterItemModel>?> GetSavedForLaterAsync(long cartId)
            => _client.GetFromJsonAsync<List<SavedForLaterItemModel>>($"cart/savedforlater/{cartId}");

        public async Task<bool> SaveForLaterAsync(long cartId, int itemId)
        {
            var payload = new { CartId = cartId, ItemId = itemId };
            using var req = new HttpRequestMessage(HttpMethod.Post, "cart/saveforlater")
            {
                Content = JsonContent.Create(payload)
            };
            AddCommonHeaders(req, $"sfl:add:{cartId}:{itemId}");
            var resp = await _client.SendAsync(req);
            return resp.IsSuccessStatusCode;
        }

        public async Task<bool> MoveSavedToCartAsync(int savedItemId)
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, $"cart/movesavedtocart/{savedItemId}");
            AddCommonHeaders(req, $"sfl:move:{savedItemId}");
            var resp = await _client.SendAsync(req);
            return resp.IsSuccessStatusCode;
        }
    }
}
