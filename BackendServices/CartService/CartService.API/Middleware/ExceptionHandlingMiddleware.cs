using Microsoft.AspNetCore.Mvc;
using OpenTelemetry.Trace;
using Serilog;
using System.Diagnostics;
using System.Text.Json;
using CartService.Application.Exceptions;

namespace CartService.API.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;
        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (AppException appEx)
            {
                var traceId = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;

                // Log severity based on status
                var level = appEx.StatusCode >= 500 ? Serilog.Events.LogEventLevel.Error :
                            appEx.StatusCode >= 400 ? Serilog.Events.LogEventLevel.Warning :
                            Serilog.Events.LogEventLevel.Information;

                Log.Write(level, appEx, "Handled application exception {Status} {Title} for {Method} {Path}",
                    appEx.StatusCode, appEx.Title, context.Request.Method, context.Request.Path);

                if (appEx.Errors is { Count: > 0 })
                {
                    var vpd = new ValidationProblemDetails(appEx.Errors)
                    {
                        Title = appEx.Title,
                        Status = appEx.StatusCode,
                        Type = appEx.Type ?? "about:blank",
                        Instance = context.TraceIdentifier,
                        Detail = appEx.Message
                    };
                    vpd.Extensions["traceId"] = traceId;

                    if (appEx.Extensions is not null)
                        foreach (var kv in appEx.Extensions) vpd.Extensions[kv.Key] = kv.Value;

                    await WriteProblemObjectAsync(context, vpd.Status!.Value, vpd);
                }
                else
                {
                    await WriteProblemAsync(context,
                        statusCode: appEx.StatusCode,
                        title: appEx.Title,
                        detail: appEx.Message,
                        type: appEx.Type ?? "about:blank",
                        extensions: appEx.Extensions);
                }
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
                // Client disconnected or request cancelled
                context.Response.StatusCode = 499; // Client Closed Request (nginx convention)
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unhandled exception");
                await WriteProblemAsync(context, StatusCodes.Status500InternalServerError,
                    title: "Unexpected error",
                    detail: "An unexpected error occurred. Try again later.",
                    type: "urn:problem:cart:unexpected");
            }
        }

        private static async Task WriteProblemAsync(HttpContext context, int statusCode, string title, string detail, string type, IDictionary<string, object>? extensions = null)
        {
            var problem = new ProblemDetails
            {
                Title = title,
                Detail = detail,
                Status = statusCode,
                Type = type,
                Instance = context.TraceIdentifier
            };

            if (extensions != null)
            {
                foreach (var kvp in extensions)
                    problem.Extensions[kvp.Key] = kvp.Value;
            }

            problem.Extensions["traceId"] = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
            await WriteProblemObjectAsync(context, statusCode, problem);
        }

        private static async Task WriteProblemObjectAsync(HttpContext context, int status, ProblemDetails problem)
        {
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
        {
            return builder.UseMiddleware<ExceptionHandlingMiddleware>();
        }
    }
}
