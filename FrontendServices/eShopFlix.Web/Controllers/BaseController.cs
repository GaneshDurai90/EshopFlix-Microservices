using eShopFlix.Web.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace eShopFlix.Web.Controllers
{
    public class BaseController : Controller
    {
        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

        public UserModel? CurrentUser
        {
            get
            {
                if (User?.Identity?.IsAuthenticated == true)
                {
                    var claim = User.FindFirst(ClaimTypes.UserData);
                    if (claim != null && !string.IsNullOrWhiteSpace(claim.Value))
                    {
                        return JsonSerializer.Deserialize<UserModel>(claim.Value, SerializerOptions);
                    }
                }
                return null;
            }
        }
    }
}
