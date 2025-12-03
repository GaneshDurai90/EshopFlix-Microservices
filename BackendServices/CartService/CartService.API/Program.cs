using CartService.Api.Extensions;
using CartService.Api.Http;
using CartService.Api.Logging;
using CartService.Api.Observability;
using CartService.API.Middleware;
using CartService.Application.HttpClients;
using CartService.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using Polly;
using Polly.Extensions.Http;
using Serilog;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);


// Logging + Observability
SerilogExtensions.ConfigureSerilog(builder.Configuration);
builder.Host.UseSerilog();
builder.Services.AddCartServiceOpenTelemetry(builder.Configuration);

// Core
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(o => o.SuppressModelStateInvalidFilter = true);
builder.Services.AddEndpointsApiExplorer();

// Swagger generation
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CartService API",
        Version = "v1",
        Description = "HTTP endpoints for CartService"
    });

    // Include XML comments for better docs (requires XML documentation file in csproj)
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    }
});

builder.Services.AddHttpContextAccessor();

// Cross-cutting
builder.Services.AddProblemDetails();
builder.Services.AddTransient<CorrelationIdDelegatingHandler>();

// Catalog client with resilience: set infinite HttpClient timeout so Polly timeout controls it
builder.Services.AddHttpClient<CatalogServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiAddress:CatalogApi"]);
    client.Timeout = Timeout.InfiniteTimeSpan; // let Polly timeout drive cancellation
})
.AddHttpMessageHandler<CorrelationIdDelegatingHandler>()
.AddPolicyHandler(PollyPolicies.CreatePolicy("catalog")); // ensure logs show op=catalog

builder.Services.AddHealthChecks();

// DI for Domain/Infra.
ServiceRegistration.RegisterServices(builder.Services, builder.Configuration);

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();



var app = builder.Build();

//Middleware Order 
// a) Correlation ID first to enrich downstream logs/requests
app.UseMiddleware<CorrelationIdMiddleware>();

// b) Serilog request logging (uses CorrelationId from LogContext)
app.UseSerilogRequestLogging(opts =>
{
    opts.EnrichDiagnosticContext = (diag, http) =>
    {
        diag.Set("TraceId", Activity.Current?.TraceId.ToString());
        diag.Set("CorrelationId", http.TraceIdentifier);
        diag.Set("RequestPath", http.Request.Path);
        diag.Set("RequestMethod", http.Request.Method);
        diag.Set("StatusCode", http.Response.StatusCode);
        diag.Set("Application", "CartService");
    };
});

// c) Global exception handler (produces RFC7807 ProblemDetails)
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Toggle-able Swagger (enabled via config Swagger:Enabled or in Development)
var enableSwagger = app.Configuration.GetValue<bool?>("Swagger:Enabled") ?? app.Environment.IsDevelopment();
if (enableSwagger)
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "CartService API v1");
        c.RoutePrefix = "swagger"; // UI at /swagger
    });

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
// Health endpoints
// 2) Map health endpoints
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    // Liveness: if the process is running, return Healthy
    Predicate = _ => false,
    ResponseWriter = WriteHealthResponse
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    // Readiness: run all registered checks (DB, SB, etc. if you enabled them)
    Predicate = _ => true,
    ResponseWriter = WriteHealthResponse
});

app.Run();

static Task WriteHealthResponse(HttpContext ctx, HealthReport report)
{
    ctx.Response.ContentType = "application/json";
    var payload = new
    {
        status = report.Status.ToString(),
        checks = report.Entries.Select(e => new
        {
            name = e.Key,
            status = e.Value.Status.ToString(),
            error = e.Value.Exception?.Message,
            duration = e.Value.Duration.TotalMilliseconds
        }),
        totalDuration = report.TotalDuration.TotalMilliseconds,
        at = DateTime.UtcNow
    };
    return ctx.Response.WriteAsync(JsonSerializer.Serialize(payload));
}
