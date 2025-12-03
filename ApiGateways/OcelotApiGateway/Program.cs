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

// Register downstream resilience delegating handler via DI so we can reference it in ocelot config
builder.Services.AddTransient<OcelotApiGateway.Resilience.DownstreamResilienceHandler>();

// Caching:
// - Distributed Redis cache for multi-instance scenarios (falls back to memory if Redis is unavailable).
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "gateway:";
});
builder.Services.AddSingleton<OcelotApiGateway.Security.AuthorizationCache>();

// Authorization caches/services
builder.Services.AddSingleton<JwksCacheService>();
builder.Services.AddSingleton<ClaimsCacheService>();

// Named HttpClient for JWKS fetch (future use when switching to asymmetric JWTs/JWKS)
builder.Services.AddHttpClient("jwks", c =>
{
    c.Timeout = TimeSpan.FromSeconds(2);
});

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
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };
    // Harden backchannel if metadata is ever fetched (safe default)
    options.BackchannelTimeout = TimeSpan.FromSeconds(2);
});

var app = builder.Build();

app.UseSerilogRequestLogging(opts =>
{
    opts.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
});

// Ensure authentication is run so HttpContext.User is populated
app.UseAuthentication();

// Authorization fallback middleware must run before Ocelot
app.UseMiddleware<AuthorizationFallbackMiddleware>();

app.MapGet("/", () => "Gateway is running");

await app.UseOcelot();
app.Run();
