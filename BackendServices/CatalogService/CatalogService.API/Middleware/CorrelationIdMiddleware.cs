using Microsoft.AspNetCore.Http;
using Serilog.Context;
using System;
using System.Diagnostics;
using System.Linq;

namespace CatalogService.API.Middleware
{
    public sealed class CorrelationIdMiddleware
    {
        public const string HeaderName = "x-correlation-id";
        private readonly RequestDelegate _next;

        public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

        public async Task Invoke(HttpContext context)
        {
            var correlationId = context.Request.Headers[HeaderName].FirstOrDefault()
                               ?? Activity.Current?.TraceId.ToString()
                               ?? Guid.NewGuid().ToString("N");

            context.Items[HeaderName] = correlationId;
            if (!context.Response.HasStarted)
            {
                context.Response.Headers[HeaderName] = correlationId;
            }

            var activity = Activity.Current;
            using (LogContext.PushProperty("CorrelationId", correlationId))
            using (LogContext.PushProperty("TraceId", activity?.TraceId.ToString()))
            using (LogContext.PushProperty("SpanId", activity?.SpanId.ToString()))
            using (LogContext.PushProperty("RequestId", context.TraceIdentifier))
            {
                await _next(context);
            }
        }
    }
}
