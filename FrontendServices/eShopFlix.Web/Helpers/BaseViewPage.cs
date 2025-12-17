using eShopFlix.Web.Models;
using Microsoft.AspNetCore.Mvc.Razor;
using System.Security.Claims;
using System.Text.Json;

namespace eShopFlix.Web.Helpers
{
    public abstract class BaseViewPage<TModel> : RazorPage<TModel>
    {
        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

        public UserModel CurrentUser
        {
            get
            {
                if (User.Identity.IsAuthenticated)
                {
                    string strData = User.FindFirst(ClaimTypes.UserData).Value;
                    return JsonSerializer.Deserialize<UserModel>(strData, SerializerOptions);
                }
                return null;
            }
        }
    }
}