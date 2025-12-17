using System.Net.Http.Json;
using System.Text.Json;
using eShopFlix.Web.Models;
using Microsoft.Extensions.Logging;

namespace eShopFlix.Web.HttpClients
{
    public class AuthServiceClient
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly HttpClient _httpClient;
        private readonly ILogger<AuthServiceClient> _logger;
        
        public AuthServiceClient(HttpClient httpClient, ILogger<AuthServiceClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<UserModel?> LoginAsync(LoginModel model, CancellationToken cancellationToken = default)
        {
            try
            {
                using var response = await _httpClient.PostAsJsonAsync("auth/login", model, JsonOptions, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogDebug("Login failed with status {StatusCode}", response.StatusCode);
                    return null;
                }

                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                return await JsonSerializer.DeserializeAsync<UserModel>(stream, JsonOptions, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Login request failed - auth service may be unavailable");
                return null;
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogWarning(ex, "Login request timed out");
                return null;
            }
        }

        public async Task<TokenResponseModel?> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return null;
            }

            try
            {
                var payload = new RefreshTokenRequestModel { RefreshToken = refreshToken };
                using var response = await _httpClient.PostAsJsonAsync("auth/refresh", payload, JsonOptions, cancellationToken);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogDebug("Token refresh failed with status {StatusCode} - token may be expired or revoked", response.StatusCode);
                    return null;
                }

                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                return await JsonSerializer.DeserializeAsync<TokenResponseModel>(stream, JsonOptions, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Token refresh request failed - auth service may be unavailable");
                return null;
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogWarning(ex, "Token refresh request timed out");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during token refresh");
                return null;
            }
        }

        public async Task<bool> RevokeAsync(string refreshToken, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return false;
            }

            try
            {
                var payload = new RefreshTokenRequestModel { RefreshToken = refreshToken };
                using var response = await _httpClient.PostAsJsonAsync("auth/revoke", payload, JsonOptions, cancellationToken);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Token revocation failed - continuing anyway");
                return false;
            }
        }

        public async Task<bool> RegisterAsync(SignUpModel model, CancellationToken cancellationToken = default)
        {
            try
            {
                using var response = await _httpClient.PostAsJsonAsync("auth/SignUp", model, JsonOptions, cancellationToken);
                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Registration request failed - auth service may be unavailable");
                return false;
            }
        }
    }
}
