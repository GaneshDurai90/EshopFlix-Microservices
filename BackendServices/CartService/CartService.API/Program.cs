using CartService.Api.Http;
using CartService.Api.Logging;
using CartService.Api.Observability;
using CartService.API.Middleware;
using CartService.Application.HttpClients;
using CartService.Infrastructure;
using Serilog;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);


// Logging + Observability
SerilogExtensions.ConfigureSerilog(builder.Configuration);
builder.Host.UseSerilog();
builder.Services.AddCartServiceOpenTelemetry(builder.Configuration);

// Core
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(o => o.SuppressModelStateInvalidFilter = true);
builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();

// Cross-cutting
builder.Services.AddProblemDetails();
builder.Services.AddTransient<CorrelationIdDelegatingHandler>();
builder.Services.AddHttpClient<CatalogServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiAddress:CatalogApi"]);

}).AddHttpMessageHandler<CorrelationIdDelegatingHandler>(); ;

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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
// Health endpoints
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");

app.Run();
