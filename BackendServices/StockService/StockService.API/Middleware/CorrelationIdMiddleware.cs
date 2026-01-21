using Microsoft.AspNetCore.Http;
using Serilog.Context;
using System.Diagnostics;

namespace StockService.API.Middleware;

/// <summary>
/// Middleware for handling correlation IDs in requests.
/// Extracts correlation ID from headers or creates a new one, making it available throughout the request pipeline.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    public const string HeaderName = "x-correlation-id";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task Invoke(HttpContext context)
    {
        // Try to get correlation ID from header, activity, or generate new one
        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault()
                          ?? Activity.Current?.TraceId.ToString()
                          ?? Guid.NewGuid().ToString("N");

        // Store in context items for access in handlers
        context.Items[HeaderName] = correlationId;
        
        // Add to response headers
        if (!context.Response.HasStarted)
        {
            context.Response.Headers[HeaderName] = correlationId;
        }

        var activity = Activity.Current;
        
        // Enrich log context with tracing information
        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("TraceId", activity?.TraceId.ToString()))
        using (LogContext.PushProperty("SpanId", activity?.SpanId.ToString()))
        using (LogContext.PushProperty("RequestId", context.TraceIdentifier))
        {
            await _next(context);
        }
    }
}

public static class CorrelationIdMiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
        => app.UseMiddleware<CorrelationIdMiddleware>();
}
