using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace OcelotApiGateway.Security
{
    public sealed class AuthorizationFallbackMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly AuthorizationCache _cache;
        private readonly ILogger<AuthorizationFallbackMiddleware> _logger;

        public AuthorizationFallbackMiddleware(RequestDelegate next, AuthorizationCache cache, ILogger<AuthorizationFallbackMiddleware> logger)
        {
            _next = next;
            _cache = cache;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var routeKey = BuildRouteKey(context.Request);
            var userId = GetUserId(context.User);

            // If user is authenticated, evaluate and persist decision
            if (context.User?.Identity?.IsAuthenticated == true && userId is not null)
            {
                var isAllowed = EvaluatePolicy(context, routeKey);
                await _cache.SetGrantAsync(userId, routeKey, isAllowed, TimeSpan.FromMinutes(10));

                if (!isAllowed)
                {
                    _logger.LogWarning("Authorization denied. userId={UserId} routeKey={RouteKey}", userId, routeKey);
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsync("Forbidden");
                    return;
                }

                await _next(context);
                return;
            }

            // Not authenticated: attempt cached fallback
            if (userId is not null)
            {
                var cached = await _cache.TryGetGrantAsync(userId, routeKey);
                if (cached is true)
                {
                    _logger.LogInformation("Authorization fallback grant used. userId={UserId} routeKey={RouteKey}", userId, routeKey);
                    await _next(context);
                    return;
                }
            }

            // No cached grant -> deny
            _logger.LogWarning("Authorization fallback denied. userId={UserId} routeKey={RouteKey}", userId ?? "unknown", routeKey);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("Forbidden");
        }

        private static string BuildRouteKey(HttpRequest request)
        {
            var path = request.Path.Value?.ToLowerInvariant() ?? "/";
            return $"{request.Method}:{path}";
        }

        private static string? GetUserId(ClaimsPrincipal user)
        {
            return user.FindFirst("sub")?.Value
                   ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        // Minimal policy aligned to ocelot-dev.json: /product/* requires Admin role; others require authentication only
        private static bool EvaluatePolicy(HttpContext context, string routeKey)
        {
            var requiresAdmin = routeKey.Contains("/product/");
            if (!requiresAdmin) return true;
            return context.User.IsInRole("Admin");
        }
    }
}