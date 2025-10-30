using eShopFlix.Web.Models;
using System.Text;
using System.Text.Json;

namespace eShopFlix.Web.HttpClients
{
    public class PaymentServiceClient
    {
        private readonly HttpClient _client;
        public PaymentServiceClient(HttpClient client)
        {
            _client = client;  
        }

        public async Task<string?> CreateOrderAsync(RazorPayOrderModel orderModel)
        {
            using var content = new StringContent(JsonSerializer.Serialize(orderModel), Encoding.UTF8, "application/json");
            using var response = await _client.PostAsync("payment/CreateOrder", content);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            return null;
        }
        public async Task<string> VerifyPaymentAsync(PaymentConfirmModel model)
        {
            using var content = new StringContent(JsonSerializer.Serialize(model), Encoding.UTF8, "application/json");
            using var response = await _client.PostAsync("payment/VerifyPayment", content);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            return string.Empty;
        }

        public async Task<bool> SavePaymentDetailsAsync(TransactionModel model)
        {
            using var content = new StringContent(JsonSerializer.Serialize(model), Encoding.UTF8, "application/json");
            using var response = await _client.PostAsync("payment/SavePaymentDetails", content);
            return response.IsSuccessStatusCode;
        }
    }
}
