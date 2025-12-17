using System.Security.Claims;
using System.Text.Json;
using eShopFlix.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;

namespace eShopFlix.Web.Services
{
    public interface IAuthTicketService
    {
        Task SignInAsync(HttpContext context, UserModel user);
        Task RefreshAsync(HttpContext context, TokenResponseModel tokens);
    }

    public sealed class AuthTicketService : IAuthTicketService
    {
        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

        public async Task SignInAsync(HttpContext context, UserModel user)
        {
            var claims = BuildClaims(user);
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(60)
            };

            await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);
        }

        public async Task RefreshAsync(HttpContext context, TokenResponseModel tokens)
        {
            if (context.User?.Identity?.IsAuthenticated != true)
            {
                return;
            }

            var payload = context.User.FindFirst(ClaimTypes.UserData)?.Value;
            if (string.IsNullOrWhiteSpace(payload))
            {
                return;
            }

            var user = JsonSerializer.Deserialize<UserModel>(payload, SerializerOptions);
            if (user == null)
            {
                return;
            }

            user.Token = tokens.AccessToken;
            user.AccessTokenExpiresAt = tokens.AccessTokenExpiresAt;
            user.RefreshToken = tokens.RefreshToken;
            user.RefreshTokenExpiresAt = tokens.RefreshTokenExpiresAt;

            await SignInAsync(context, user);
        }

        private static IEnumerable<Claim> BuildClaims(UserModel user)
        {
            var serialized = JsonSerializer.Serialize(user, SerializerOptions);
            yield return new Claim(ClaimTypes.UserData, serialized);
            yield return new Claim(ClaimTypes.Email, user.Email ?? string.Empty);

            if (user.Roles != null)
            {
                foreach (var role in user.Roles)
                {
                    if (!string.IsNullOrWhiteSpace(role))
                    {
                        yield return new Claim(ClaimTypes.Role, role);
                    }
                }
            }
        }
    }
}
