using Serilog.Context;
using System.Diagnostics;

namespace CartService.API.Middleware
{
    public sealed class CorrelationIdMiddleware
    {
        public const string HeaderName = "x-correlation-id";
        private readonly RequestDelegate _next;

        public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

        public async Task Invoke(HttpContext context)
        {
            // Prefer incoming header; else use current traceId; else new guid
            var correlationId = context.Request.Headers[HeaderName].FirstOrDefault()
                             ?? Activity.Current?.TraceId.ToString()
                             ?? Guid.NewGuid().ToString("N");

            context.Items[HeaderName] = correlationId;
            context.Response.Headers[HeaderName] = correlationId;

            using (LogContext.PushProperty("CorrelationId", correlationId))
            {
                await _next(context);
            }
        }
    }
}
