using Microsoft.IdentityModel.Tokens;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Polly;
using System.Text;
using Serilog;
using OcelotApiGateway.Logging;
using OcelotApiGateway.Security;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using System.Collections.Generic;

var builder = WebApplication.CreateBuilder(args);

// Serilog for gateway
SerilogExtensions.ConfigureSerilog(builder.Configuration);
builder.Host.UseSerilog();

// Load ocelot config per environment
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddJsonFile("ocelot-dev.json");
}
else
{
    builder.Configuration.AddJsonFile("ocelot.json");
}

builder.Services.AddOcelot().AddPolly();

// Configure request limits for security
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10 MB max
    options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(30);
});

// CORS for frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                      ?? new[] { "https://localhost:7135" }; // Default to frontend URL
        policy.WithOrigins(origins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Register downstream resilience delegating handler via DI so we can reference it in ocelot config
builder.Services.AddTransient<OcelotApiGateway.Resilience.DownstreamResilienceHandler>();

// Caching:
// - Try Redis for distributed caching with graceful fallback to memory cache if unavailable.
var redisConnection = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrWhiteSpace(redisConnection))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnection;
        options.InstanceName = "gateway:";
        // Note: We use the connection string directly. The StackExchangeRedis client
        // handles reconnection automatically with AbortOnConnectFail=false by default
        // when using AddStackExchangeRedisCache. Timeouts are handled at the operation level
        // by the AuthorizationCache wrapper which gracefully degrades on Redis failures.
    });
}
else
{
    // No Redis configured - use memory cache
    Log.Information("Redis connection string not configured, using in-memory distributed cache");
    builder.Services.AddDistributedMemoryCache();
}
builder.Services.AddSingleton<OcelotApiGateway.Security.AuthorizationCache>();

// Authorization caches/services
builder.Services.AddSingleton<JwksCacheService>();
builder.Services.AddSingleton<ClaimsCacheService>();

// Named HttpClient for JWKS fetch (future use when switching to asymmetric JWTs/JWKS)
builder.Services.AddHttpClient("jwks", c =>
{
    c.Timeout = TimeSpan.FromSeconds(2);
});

var signingKeys = new Dictionary<string, SymmetricSecurityKey>(StringComparer.OrdinalIgnoreCase);
foreach (var child in builder.Configuration.GetSection("Jwt:Keys").GetChildren())
{
    if (!string.IsNullOrWhiteSpace(child.Value))
    {
        signingKeys[child.Key] = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(child.Value));
    }
}

var legacyKey = builder.Configuration["Jwt:Key"];
if (!string.IsNullOrWhiteSpace(legacyKey) && !signingKeys.ContainsKey("legacy"))
{
    signingKeys["legacy"] = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(legacyKey));
}

var endPointAuthKey = builder.Configuration["Keys:EndpointAuthKey"];
builder.Services.AddAuthentication().AddJwtBearer(endPointAuthKey, options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKeyResolver = (token, securityToken, kid, validationParameters) =>
        {
            if (!string.IsNullOrWhiteSpace(kid) && signingKeys.TryGetValue(kid, out var key))
            {
                return new[] { key };
            }

            return signingKeys.Values;
        }
    };
    // Harden backchannel if metadata is ever fetched (safe default)
    options.BackchannelTimeout = TimeSpan.FromSeconds(2);
});

var app = builder.Build();

app.UseSerilogRequestLogging(opts =>
{
    opts.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
});

// Apply CORS before authentication
app.UseCors("AllowFrontend");

// Ensure authentication is run so HttpContext.User is populated
app.UseAuthentication();

// Example: do NOT enforce fallback auth for /catalog in dev
// Also skip /cart for now during development - frontend handles auth via cookies
app.UseWhen(
    ctx => !ctx.Request.Path.StartsWithSegments("/catalog", StringComparison.OrdinalIgnoreCase)
           && !ctx.Request.Path.StartsWithSegments("/cart", StringComparison.OrdinalIgnoreCase) // Skip cart auth at gateway - let backend handle it
           && (ctx.Request.Path.StartsWithSegments("/product", StringComparison.OrdinalIgnoreCase)
               || ctx.Request.Path.StartsWithSegments("/payment", StringComparison.OrdinalIgnoreCase)
               || ctx.Request.Path.StartsWithSegments("/order", StringComparison.OrdinalIgnoreCase)),
    appBuilder =>
    {
        appBuilder.UseMiddleware<AuthorizationFallbackMiddleware>();
    });

// Note: AuthorizationFallbackMiddleware is applied conditionally above via UseWhen

// Handle root path before Ocelot takes over - this prevents "route not found" errors for /
app.MapGet("/", () => "Gateway is running");
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));
app.MapGet("/health/ready", async (IServiceProvider sp) =>
{
    var checks = new Dictionary<string, object>();
    var healthy = true;

    // Check Redis connectivity
    try
    {
        var cache = sp.GetService<Microsoft.Extensions.Caching.Distributed.IDistributedCache>();
        if (cache != null)
        {
            await cache.GetAsync("health-check");
            checks["redis"] = "healthy";
        }
        else
        {
            checks["redis"] = "not_configured";
        }
    }
    catch
    {
        checks["redis"] = "unhealthy";
        // Redis being unhealthy shouldn't fail the gateway - it degrades gracefully
    }

    checks["gateway"] = "healthy";

    return Results.Ok(new
    {
        status = healthy ? "healthy" : "degraded",
        timestamp = DateTime.UtcNow,
        checks
    });
});

// UseRouting must be called to enable the MapGet endpoints above
app.UseRouting();

// Map endpoints before Ocelot so they're handled first
app.UseEndpoints(endpoints =>
{
    // Endpoints are already mapped above with MapGet
});

await app.UseOcelot();
app.Run();
