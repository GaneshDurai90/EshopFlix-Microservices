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
            var requiredScope = ResolveScopeRequirement(context.Request);
            if (requiredScope is null)
            {
                return true;
            }

            return context.User.HasClaim("scope", requiredScope) || context.User.IsInRole("Admin");
        }

        private static string? ResolveScopeRequirement(HttpRequest request)
        {
            var path = request.Path.Value?.ToLowerInvariant() ?? string.Empty;

            if (path.StartsWith("/product"))
            {
                return "product.admin";
            }

            if (path.StartsWith("/cart"))
            {
                return HttpMethods.IsGet(request.Method) ? "cart.read" : "cart.write";
            }

            if (path.StartsWith("/order"))
            {
                return HttpMethods.IsGet(request.Method) ? "order.read" : "order.write";
            }

            if (path.StartsWith("/payment"))
            {
                return "payment.execute";
            }

            if (path.StartsWith("/shipping"))
            {
                return HttpMethods.IsGet(request.Method) ? "shipping.read" : "shipping.write";
            }

            if (path.StartsWith("/admin"))
            {
                return "admin.portal";
            }

            return null;
        }
    }
}