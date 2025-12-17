using eShopFlix.Web.HttpClients;
using eShopFlix.Web.Models;
using eShopFlix.Web.Security;
using eShopFlix.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace eShopFlix.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly AuthServiceClient _authServiceClient;
        private readonly IAuthTicketService _authTicketService;
        public AccountController(AuthServiceClient authServiceClient, IAuthTicketService authTicketService)
        {
            _authServiceClient = authServiceClient;
            _authTicketService = authTicketService;

        }
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginModel model, string? returnUrl)
        {
            if (ModelState.IsValid)
            {
                UserModel user = await _authServiceClient.LoginAsync(model);
                if (user != null)
                {
                    await _authTicketService.SignInAsync(HttpContext, user);
                    SetTokenCookies(user);

                    if (!string.IsNullOrEmpty(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }

                    // After login, land on the public product explore page
                    // Admins go to Admin dashboard; users go to storefront
                    if (user.Roles?.Contains("Admin") == true)
                    {
                        return RedirectToAction("Index", "Home", new { area = "Admin" });
                    }
                    else
                    {
                        return RedirectToAction("Index", "Home");
                    }
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid Username or Password");
                }
            }
            return View();
        }

        private void SetTokenCookies(UserModel user)
        {
            if (!string.IsNullOrWhiteSpace(user.Token))
            {
                Response.Cookies.Append(
                    AuthCookieNames.AccessToken,
                    user.Token,
                    new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Lax,
                        Expires = user.AccessTokenExpiresAt == default
                            ? DateTimeOffset.UtcNow.AddMinutes(30)
                            : new DateTimeOffset(DateTime.SpecifyKind(user.AccessTokenExpiresAt, DateTimeKind.Utc))
                    });
            }

            if (!string.IsNullOrWhiteSpace(user.RefreshToken))
            {
                Response.Cookies.Append(
                    AuthCookieNames.RefreshToken,
                    user.RefreshToken,
                    new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Strict,
                        Expires = user.RefreshTokenExpiresAt == default
                            ? DateTimeOffset.UtcNow.AddDays(14)
                            : new DateTimeOffset(DateTime.SpecifyKind(user.RefreshTokenExpiresAt, DateTimeKind.Utc))
                    });
            }
        }

        private void ClearTokenCookies()
        {
            Response.Cookies.Delete(AuthCookieNames.AccessToken);
            Response.Cookies.Delete(AuthCookieNames.RefreshToken);
        }

        public async Task<IActionResult> Logout()
        {
            var refreshToken = Request.Cookies[AuthCookieNames.RefreshToken];
            if (!string.IsNullOrWhiteSpace(refreshToken))
            {
                await _authServiceClient.RevokeAsync(refreshToken);
            }
            ClearTokenCookies();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        public IActionResult UnAuthorize()
        {
            return View();
        }
    }
}

