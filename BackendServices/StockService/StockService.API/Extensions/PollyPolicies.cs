using Polly;
using Polly.Timeout;
using Serilog;
using System.Net;
using System.Text;

namespace StockService.API.Extensions;

/// <summary>
/// Resilience pipeline for outbound HTTP calls using Polly.
/// Implements retry, circuit breaker, timeout, bulkhead, and fallback patterns.
/// </summary>
public static class PollyPolicies
{
    public static IAsyncPolicy<HttpResponseMessage> CreatePolicy(string operationKey)
        => BuildCompositePolicy(operationKey);

    public static IAsyncPolicy<HttpResponseMessage> CreatePolicy()
        => BuildCompositePolicy("http");

    private static IAsyncPolicy<HttpResponseMessage> BuildCompositePolicy(string operationKey)
    {
        var rng = new Random();

        // Retry policy with exponential backoff and jitter
        var retry = Policy
            .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .Or<HttpRequestException>()
            .Or<TimeoutRejectedException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, attempt)) + TimeSpan.FromMilliseconds(rng.Next(0, 250)),
                onRetry: (outcome, delay, attempt, ctx) =>
                {
                    var status = outcome.Result?.StatusCode.ToString() ?? outcome.Exception?.GetType().Name ?? "unknown";
                    Log.Warning("[Polly-Retry] op={OperationKey} attempt={Attempt} delay={Delay}s status={Status}",
                        ctx.OperationKey ?? operationKey,
                        attempt,
                        Math.Round(delay.TotalSeconds, 3),
                        status);
                }
            );

        // Circuit breaker to prevent cascading failures
        var breaker = Policy
            .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .Or<HttpRequestException>()
            .Or<TimeoutRejectedException>()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (outcome, span) =>
                {
                    var status = outcome.Result?.StatusCode.ToString() ?? outcome.Exception?.GetType().Name ?? "unknown";
                    Log.Warning("[CircuitBreaker] op={OperationKey} OPEN for {Break}s status={Status}",
                        operationKey, span.TotalSeconds, status);
                },
                onReset: () => Log.Information("[CircuitBreaker] op={OperationKey} RESET", operationKey),
                onHalfOpen: () => Log.Information("[CircuitBreaker] op={OperationKey} HALF-OPEN", operationKey)
            );

        // Timeout policy
        var timeout = Policy.TimeoutAsync<HttpResponseMessage>(
            timeout: TimeSpan.FromSeconds(20),
            timeoutStrategy: TimeoutStrategy.Optimistic,
            onTimeoutAsync: (context, span, task, ct) =>
            {
                Log.Error("[Timeout] op={OperationKey} after {Seconds}s",
                    context.OperationKey ?? operationKey, span.TotalSeconds);
                return Task.CompletedTask;
            });

        // Fallback policy for graceful degradation
        var fallback = Policy<HttpResponseMessage>
            .Handle<Exception>()
            .OrResult(r => !r.IsSuccessStatusCode)
            .FallbackAsync(
                fallbackAction: ct =>
                {
                    Log.Warning("[Fallback] op={OperationKey} returning safe response", operationKey);
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("[]", Encoding.UTF8, "application/json")
                    });
                });

        // Bulkhead for isolation
        var bulkhead = Policy.BulkheadAsync<HttpResponseMessage>(
            maxParallelization: 25,
            maxQueuingActions: 15,
            onBulkheadRejectedAsync: ctx =>
            {
                Log.Warning("[Bulkhead] op={OperationKey} rejected due to overload",
                    ctx.OperationKey ?? operationKey);
                return Task.CompletedTask;
            });

        // Compose all policies
        return Policy
            .WrapAsync(
                bulkhead,
                Policy.WrapAsync(
                    fallback,
                    Policy.WrapAsync(
                        breaker,
                        Policy.WrapAsync(retry, timeout))))
            .WithPolicyKey(operationKey);
    }

    /// <summary>
    /// Create a simple retry policy without circuit breaker (for internal operations).
    /// </summary>
    public static IAsyncPolicy<T> CreateSimpleRetryPolicy<T>(string operationKey, int retryCount = 3)
    {
        var rng = new Random();
        return Policy<T>
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: retryCount,
                sleepDurationProvider: attempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, attempt)) + TimeSpan.FromMilliseconds(rng.Next(0, 100)),
                onRetry: (outcome, delay, attempt, ctx) =>
                {
                    Log.Warning("[SimpleRetry] op={OperationKey} attempt={Attempt} delay={Delay}s exception={Exception}",
                        ctx.OperationKey ?? operationKey,
                        attempt,
                        Math.Round(delay.TotalSeconds, 3),
                        outcome.Exception?.GetType().Name ?? "none");
                }
            );
    }
}
