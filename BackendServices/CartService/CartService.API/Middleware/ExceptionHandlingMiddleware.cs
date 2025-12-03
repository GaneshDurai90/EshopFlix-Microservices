using Microsoft.AspNetCore.Mvc;             
using Serilog;
using System.Diagnostics;
using System.Text.Json;
using CartService.Application.Exceptions;
using System.Security.Claims;
using Polly.Timeout;


namespace CartService.API.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;
        private const string CorrKey = "x-correlation-id";

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var correlationId =
                context.Items[CorrKey] as string ??
                context.Request.Headers[CorrKey].FirstOrDefault() ??
                context.TraceIdentifier;

            try
            {
                await _next(context);
            }
            catch (AppException appEx)
            {
                var traceId = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
                var userId = context.User?.Identity?.IsAuthenticated == true
                    ? context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                    : null;

                Log.ForContext("TraceId", traceId)
                   .ForContext("CorrelationId", correlationId)
                   .ForContext("UserId", userId)
                   .ForContext("RequestPath", context.Request.Path.Value)
                   .ForContext("RequestMethod", context.Request.Method)
                   .ForContext("StatusCode", appEx.StatusCode)
                   .Write(appEx.StatusCode >= 500 ? Serilog.Events.LogEventLevel.Error
                                                  : appEx.StatusCode >= 400 ? Serilog.Events.LogEventLevel.Information
                                                                            : Serilog.Events.LogEventLevel.Warning,
                          appEx, "Handled application exception {Title}", appEx.Title);

                var extensions = new Dictionary<string, object?>(appEx.Extensions ?? new Dictionary<string, object?>());
                if (appEx.Errors is not null) extensions["errors"] = appEx.Errors;

                await WriteProblemAsync(context,
                    statusCode: appEx.StatusCode,
                    title: appEx.Title,
                    detail: appEx.Message,
                    type: appEx.Type ?? "about:blank",
                    correlationId: correlationId,
                    traceId: traceId,
                    extensions: extensions);
            }
            catch (OperationCanceledException oce) when (context.RequestAborted.IsCancellationRequested)
            {
                var traceId = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;

                Log.ForContext("TraceId", traceId)
                   .ForContext("CorrelationId", correlationId)
                   .ForContext("RequestPath", context.Request.Path.Value)
                   .ForContext("RequestMethod", context.Request.Method)
                   .Information(oce, "Request cancelled by client/gateway");

                // Avoid writing a body on client-abort; just set status if possible.
                if (!context.Response.HasStarted)
                {
                    context.Response.StatusCode = 499;
                }
            }
            catch (TimeoutRejectedException tex)
            {
                var traceId = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;

                Log.ForContext("TraceId", traceId)
                   .ForContext("CorrelationId", correlationId)
                   .ForContext("RequestPath", context.Request.Path.Value)
                   .ForContext("RequestMethod", context.Request.Method)
                   .Warning(tex, "Request timed out waiting for external dependency");

                var problem = new ProblemDetails
                {
                    Type = "https://httpstatuses.com/408",
                    Title = "Request timeout",
                    Status = StatusCodes.Status408RequestTimeout,
                    Detail = "The operation timed out while waiting for an external dependency to respond.",
                    Instance = context.Request.Path
                };
                problem.Extensions["traceId"] = traceId;
                problem.Extensions["correlationId"] = correlationId;

                await WriteProblemObjectAsync(context, StatusCodes.Status408RequestTimeout, problem);
            }
            catch (Exception ex)
            {
                var traceId = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;

                Log.ForContext("TraceId", traceId)
                   .ForContext("CorrelationId", correlationId)
                   .ForContext("RequestPath", context.Request.Path.Value)
                   .ForContext("RequestMethod", context.Request.Method)
                   .ForContext("StatusCode", 500)
                   .Error(ex, "Unhandled exception");

                await WriteProblemAsync(context, StatusCodes.Status500InternalServerError,
                    title: "Unexpected error",
                    detail: "An unexpected error occurred. Try again later.",
                    type: "urn:problem:cart:unexpected",
                    correlationId: correlationId,
                    traceId: traceId);
            }
        }

        private static async Task WriteProblemAsync(
            HttpContext context,
            int statusCode,
            string title,
            string detail,
            string type,
            string? correlationId = null,
            string? traceId = null,
            IDictionary<string, object?>? extensions = null)
        {
            var problem = new ProblemDetails
            {
                Title = title,
                Detail = detail,
                Status = statusCode,
                Type = type,
                Instance = context.Request.Path
            };

            if (extensions is not null)
            {
                foreach (var kvp in extensions) problem.Extensions[kvp.Key] = kvp.Value;
            }

            problem.Extensions["traceId"] = traceId ?? Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
            if (!string.IsNullOrWhiteSpace(correlationId))
                problem.Extensions["correlationId"] = correlationId;

            await WriteProblemObjectAsync(context, statusCode, problem);
        }

        private static async Task WriteProblemObjectAsync(HttpContext context, int status, ProblemDetails problem)
        {
            if (context.Response.HasStarted) return;

            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = status;

            var json = JsonSerializer.Serialize(problem, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });
            await context.Response.WriteAsync(json);
        }
    }

    public static class ExceptionHandlingMiddlewareExtensions
    {
        public static IApplicationBuilder UseExceptionHandlingMiddleware(this IApplicationBuilder builder)
            => builder.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}
