using System;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using eShopFlix.Web.HttpClients;
using eShopFlix.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace eShopFlix.Web.Filters
{
    public sealed class CartInfoFilter : IAsyncResultFilter
    {
        private readonly CartServiceClient _cartServiceClient;
        private readonly ILogger<CartInfoFilter> _logger;
        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

        public CartInfoFilter(CartServiceClient cartServiceClient, ILogger<CartInfoFilter> logger)
        {
            _cartServiceClient = cartServiceClient;
            _logger = logger;
        }

        public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            try
            {
                if (context.Result is ViewResult or PartialViewResult)
                {
                    var userClaim = context.HttpContext.User.FindFirst(ClaimTypes.UserData);
                    if (userClaim != null)
                    {
                        var user = JsonSerializer.Deserialize<UserModel>(userClaim.Value, SerializerOptions);
                        if (user != null)
                        {
                            const string cacheKey = "CartItemCount";
                            if (!context.HttpContext.Items.TryGetValue(cacheKey, out var cached) || cached is not int cachedCount)
                            {
                                cachedCount = await _cartServiceClient.GetCartItemCount(user.UserId);
                                context.HttpContext.Items[cacheKey] = cachedCount;
                            }

                            if (context.Controller is Controller controller)
                            {
                                controller.ViewData[cacheKey] = cachedCount;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Unable to resolve cart count for badge.");
            }

            await next();
        }
    }
}
