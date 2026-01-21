using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using eShopFlix.Web.HttpClients;
using eShopFlix.Web.Models;
using eShopFlix.Web.Security;
using eShopFlix.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace eShopFlix.Web.HttpClients
{
    public class JwtForwardingHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly AuthServiceClient _authServiceClient;
        private readonly IAuthTicketService _authTicketService;
        private readonly ILogger<JwtForwardingHandler> _logger;
        
        // Track refresh attempts to prevent cascading failures
        private static readonly SemaphoreSlim _refreshLock = new(1, 1);

        public JwtForwardingHandler(
            IHttpContextAccessor httpContextAccessor, 
            AuthServiceClient authServiceClient, 
            IAuthTicketService authTicketService,
            ILogger<JwtForwardingHandler> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _authServiceClient = authServiceClient;
            _authTicketService = authTicketService;
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var context = _httpContextAccessor.HttpContext;
            if (context != null)
            {
                var accessToken = context.Request.Cookies[AuthCookieNames.AccessToken];
                var refreshToken = context.Request.Cookies[AuthCookieNames.RefreshToken];

                if (!string.IsNullOrWhiteSpace(accessToken))
                {
                    // Don't try to refresh if cancellation is already requested
                    if (!cancellationToken.IsCancellationRequested && 
                        TokenNeedsRefresh(accessToken) && 
                        !string.IsNullOrWhiteSpace(refreshToken))
                    {
                        try
                        {
                            var refreshed = await TryRefreshWithLockAsync(refreshToken, context, cancellationToken);
                            if (refreshed != null)
                            {
                                accessToken = refreshed.AccessToken;
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            // Cancellation requested, proceed with existing token
                            _logger.LogDebug("Token refresh skipped due to cancellation");
                        }
                        // If refresh failed, still try with the existing token (might still be valid)
                    }

                    if (!string.IsNullOrWhiteSpace(accessToken))
                    {
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                    }
                }
                else if (!string.IsNullOrWhiteSpace(refreshToken) && !cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        var refreshed = await TryRefreshWithLockAsync(refreshToken, context, cancellationToken);
                        if (refreshed != null)
                        {
                            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", refreshed.AccessToken);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // Cancellation requested, proceed without token
                        _logger.LogDebug("Token refresh skipped due to cancellation");
                    }
                }
            }

            return await base.SendAsync(request, cancellationToken);
        }

        private static bool TokenNeedsRefresh(string token)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                if (!handler.CanReadToken(token))
                {
                    return false;
                }

                var jwt = handler.ReadJwtToken(token);
                return jwt.ValidTo <= DateTime.UtcNow.AddMinutes(1);
            }
            catch
            {
                return false;
            }
        }

        private async Task<TokenResponseModel?> TryRefreshWithLockAsync(string refreshToken, HttpContext context, CancellationToken cancellationToken)
        {
            // Use a separate cancellation token for refresh - don't let caller's short timeout kill refresh
            // Token refresh should have its own reasonable timeout
            using var refreshCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            
            // But still respect if the original token is cancelled
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(refreshCts.Token);
            
            // Use a short lock timeout - if another request is refreshing, just use existing token
            var acquired = await _refreshLock.WaitAsync(TimeSpan.FromMilliseconds(500), linkedCts.Token);
            if (!acquired)
            {
                _logger.LogDebug("Could not acquire refresh lock quickly, using existing token");
                return null;
            }

            try
            {
                // Check if token was already refreshed by another request
                var currentAccessToken = context.Request.Cookies[AuthCookieNames.AccessToken];
                if (!string.IsNullOrWhiteSpace(currentAccessToken) && !TokenNeedsRefresh(currentAccessToken))
                {
                    return new TokenResponseModel { AccessToken = currentAccessToken };
                }

                // Use the refresh-specific cancellation token, not the caller's
                return await TryRefreshAsync(refreshToken, context, refreshCts.Token);
            }
            finally
            {
                _refreshLock.Release();
            }
        }

        private async Task<TokenResponseModel?> TryRefreshAsync(string refreshToken, HttpContext context, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _authServiceClient.RefreshAsync(refreshToken, cancellationToken);
                if (response == null)
                {
                    _logger.LogDebug("Token refresh returned null - refresh token may be invalid or expired");
                    await HandleRefreshFailureAsync(context, cancellationToken);
                    return null;
                }

                SetTokenCookies(context, response);
                await _authTicketService.RefreshAsync(context, response);
                _logger.LogDebug("Token refresh successful");
                return response;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Token refresh failed due to HTTP error - auth service may be unavailable");
                // Don't clear cookies on network errors - the token might still be valid
                return null;
            }
            catch (OperationCanceledException ex)
            {
                // Handle both TaskCanceledException and OperationCanceledException
                _logger.LogDebug(ex, "Token refresh cancelled or timed out");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during token refresh");
                return null;
            }
        }

        private static void SetTokenCookies(HttpContext context, TokenResponseModel tokens)
        {
            context.Response.Cookies.Append(
                AuthCookieNames.AccessToken,
                tokens.AccessToken,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Lax,
                    Expires = tokens.AccessTokenExpiresAt == default
                        ? DateTimeOffset.UtcNow.AddMinutes(30)
                        : new DateTimeOffset(DateTime.SpecifyKind(tokens.AccessTokenExpiresAt, DateTimeKind.Utc))
                });

            context.Response.Cookies.Append(
                AuthCookieNames.RefreshToken,
                tokens.RefreshToken,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = tokens.RefreshTokenExpiresAt == default
                        ? DateTimeOffset.UtcNow.AddDays(14)
                        : new DateTimeOffset(DateTime.SpecifyKind(tokens.RefreshTokenExpiresAt, DateTimeKind.Utc))
                });
        }

        private static void ClearTokenCookies(HttpContext context)
        {
            context.Response.Cookies.Delete(AuthCookieNames.AccessToken);
            context.Response.Cookies.Delete(AuthCookieNames.RefreshToken);
        }

        private static void SetAuthErrorCookie(HttpContext context, string value)
        {
            context.Response.Cookies.Append(
                AuthCookieNames.AuthError,
                value,
                new CookieOptions
                {
                    HttpOnly = false,
                    Secure = true,
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTimeOffset.UtcNow.AddMinutes(5)
                });
        }

        private async Task HandleRefreshFailureAsync(HttpContext context, CancellationToken cancellationToken)
        {
            if (context == null)
            {
                return;
            }

            ClearTokenCookies(context);
            SetAuthErrorCookie(context, "refresh_failed");
            
            try
            {
                await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error signing out after refresh failure - may already be signed out");
            }
        }
    }
}
