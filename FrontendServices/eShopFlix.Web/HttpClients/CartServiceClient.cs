using System.Text.Json;
using System.Text;
using eShopFlix.Web.Models;

namespace eShopFlix.Web.HttpClients
{
    public class CartServiceClient
    {
        private readonly HttpClient _client;
        public CartServiceClient(HttpClient client)
        {
            _client = client;
        }

        public async Task<CartModel?> GetUserCartAsync(long UserId)
        {
            using var response = await _client.GetAsync($"cart/getusercart/{UserId}");
            if (response.IsSuccessStatusCode)
            {
                string responseContent = await response.Content.ReadAsStringAsync();
                if (!string.IsNullOrWhiteSpace(responseContent))
                {
                    return JsonSerializer.Deserialize<CartModel>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
            }
            return null;
        }
        public async Task<CartModel?> GetCartAsync(long CartId)
        {
            return await _client.GetFromJsonAsync<CartModel>($"cart/getcart/{CartId}");
        }

        public async Task<bool> MakeCartInActiveAsync(long CartId)
        {
            return await _client.GetFromJsonAsync<bool>($"cart/makeinactive/{CartId}");
        }

        public async Task<CartModel?> AddToCartAsync(CartItemModel item, long UserId)
        {
            using var content = new StringContent(JsonSerializer.Serialize(item), Encoding.UTF8, "application/json");
            using var response = await _client.PostAsync($"cart/additem/{UserId}", content);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<CartModel>();
            }
            return null;
        }

        public async Task<int> DeleteCartItemAsync(long CartId, int ItemId)
        {
            // Ocelot does not support DeleteFromJsonAsync; use Send and parse content
            using var request = new HttpRequestMessage(HttpMethod.Delete, $"cart/deleteItem/{CartId}/{ItemId}");
            using var response = await _client.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                if (int.TryParse(json, out var value)) return value;
                return await response.Content.ReadFromJsonAsync<int>();
            }
            return 0;
        }

        public async Task<int> UpdateQuantity(long CartId, int ItemId, int Quantity)
        {
            return await _client.GetFromJsonAsync<int>($"cart/UpdateQuantity/{CartId}/{ItemId}/{Quantity}");
        }

        public async Task<int> GetCartItemCount(long UserId)
        {
            try
            {
                return await _client.GetFromJsonAsync<int>($"cart/GetCartItemCount/{UserId}");
            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching cart item count", ex);
            }
        }

        // ====== New endpoints for expanded cart features ======
        public Task<CartTotalsModel?> GetTotalsAsync(long cartId)
            => _client.GetFromJsonAsync<CartTotalsModel>($"cart/totals/{cartId}");

        public Task<List<CouponModel>?> GetCouponsAsync(long cartId)
            => _client.GetFromJsonAsync<List<CouponModel>>($"cart/coupons/{cartId}");

        public async Task<bool> ApplyCouponAsync(long cartId, string code, decimal amount, string? description = null)
        {
            var payload = new { CartId = cartId, Code = code, Amount = amount, Description = description };
            var res = await _client.PostAsJsonAsync("cart/applycoupon", payload);
            return res.IsSuccessStatusCode;
        }

        public async Task<bool> RemoveCouponAsync(long cartId, string code)
        {
            var res = await _client.DeleteAsync($"cart/removecoupon/{cartId}/{code}");
            return res.IsSuccessStatusCode;
        }

        public Task<List<ShipmentModel>?> GetShipmentsAsync(long cartId)
            => _client.GetFromJsonAsync<List<ShipmentModel>>($"cart/shipments/{cartId}");

        public async Task<bool> SelectShippingAsync(long cartId, ShipmentModel method, string? addressSnapshotJson = null)
        {
            var payload = new
            {
                CartId = cartId,
                Carrier = method.Carrier,
                MethodCode = method.MethodCode,
                MethodName = method.MethodName,
                Cost = method.Cost,
                EstimatedDays = method.EstimatedDays,
                AddressSnapshotJson = addressSnapshotJson
            };
            var res = await _client.PostAsJsonAsync("cart/selectshipping", payload);
            return res.IsSuccessStatusCode;
        }

        public Task<List<TaxLineModel>?> GetTaxesAsync(long cartId)
            => _client.GetFromJsonAsync<List<TaxLineModel>>($"cart/taxes/{cartId}");

        public async Task<bool> ClearAsync(long cartId)
        {
            var res = await _client.PostAsync($"cart/clear/{cartId}", null);
            return res.IsSuccessStatusCode;
        }

        // Save for later
        public Task<List<SavedForLaterItemModel>?> GetSavedForLaterAsync(long cartId)
            => _client.GetFromJsonAsync<List<SavedForLaterItemModel>>($"cart/savedforlater/{cartId}");

        public Task<bool> SaveForLaterAsync(long cartId, int itemId)
            => _client.PostAsJsonAsync("cart/saveforlater", new { CartId = cartId, ItemId = itemId }).ContinueWith(t => t.Result.IsSuccessStatusCode);

        public Task<bool> MoveSavedToCartAsync(int savedItemId)
            => _client.PostAsync($"cart/movesavedtocart/{savedItemId}", null).ContinueWith(t => t.Result.IsSuccessStatusCode);
    }
}
