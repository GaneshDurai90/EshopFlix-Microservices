using CatalogService.API.Contracts;
using CatalogService.API.Middleware;
using CatalogService.Api.Extensions;
using CatalogService.Api.Http;
using CatalogService.Api.Logging;
using CatalogService.Api.Observability;
using CatalogService.Infrastructure;
using CatalogService.Application.Products.Validators;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Diagnostics;
using System.Net.Http;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;

var builder = WebApplication.CreateBuilder(args);

SerilogExtensions.ConfigureSerilog(builder.Configuration);
builder.Host.UseSerilog();
builder.Services.AddCatalogServiceOpenTelemetry(builder.Configuration);

ServiceRegistration.RegisterServices(builder.Services, builder.Configuration);

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(o => o.SuppressModelStateInvalidFilter = true);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CatalogService API",
        Version = "v1",
        Description = "HTTP endpoints for CatalogService"
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    }
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddValidatorsFromAssemblyContaining<GetProductsByIdsRequestValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateProductCommandValidator>();
builder.Services.AddProblemDetails();
builder.Services.AddTransient<CorrelationIdDelegatingHandler>();

builder.Services.AddHttpClient("catalog-dependency", client =>
{
    var dependencyBaseAddress = builder.Configuration["ApiAddress:StockApi"];
    if (!string.IsNullOrWhiteSpace(dependencyBaseAddress))
    {
        client.BaseAddress = new Uri(dependencyBaseAddress);
    }
    client.Timeout = Timeout.InfiniteTimeSpan;
})
.AddHttpMessageHandler<CorrelationIdDelegatingHandler>()
.AddPolicyHandler(PollyPolicies.CreatePolicy("stock"));

builder.Services.AddHealthChecks();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging(opts =>
{
    opts.EnrichDiagnosticContext = (diag, http) =>
    {
        diag.Set("TraceId", Activity.Current?.TraceId.ToString());
        diag.Set("CorrelationId", http.TraceIdentifier);
        diag.Set("RequestPath", http.Request.Path);
        diag.Set("RequestMethod", http.Request.Method);
        diag.Set("StatusCode", http.Response.StatusCode);
        diag.Set("Application", "CatalogService");
    };
});
app.UseMiddleware<ExceptionHandlingMiddleware>();

var enableSwagger = app.Configuration.GetValue<bool?>("Swagger:Enabled") ?? app.Environment.IsDevelopment();
if (enableSwagger)
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "CatalogService API v1");
        c.RoutePrefix = "swagger";
    });

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = WriteHealthResponse
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = WriteHealthResponse
});

app.Run();

static Task WriteHealthResponse(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json";
    var payload = new
    {
        status = report.Status.ToString(),
        checks = report.Entries.Select(entry => new
        {
            name = entry.Key,
            status = entry.Value.Status.ToString(),
            error = entry.Value.Exception?.Message,
            duration = entry.Value.Duration.TotalMilliseconds
        }),
        totalDuration = report.TotalDuration.TotalMilliseconds,
        at = DateTime.UtcNow
    };

    return context.Response.WriteAsync(JsonSerializer.Serialize(payload));
}
