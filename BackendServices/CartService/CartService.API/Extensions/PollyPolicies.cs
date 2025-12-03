using Polly;
using Polly.Timeout;
using Serilog;
using System.Net;
using System.Text;

namespace CartService.Api.Extensions
{
    /// <summary>
    /// Central resilience policy for Cart's outbound HTTP calls.
    /// Wraps: Bulkhead -> Fallback -> CircuitBreaker -> Retry -> Timeout.
    /// </summary>
    public static class PollyPolicies
    {
        public static IAsyncPolicy<HttpResponseMessage> CreatePolicy(string operationKey)
            => BuildCompositePolicy(operationKey);

        public static IAsyncPolicy<HttpResponseMessage> CreatePolicy()
            => BuildCompositePolicy("http");

        private static IAsyncPolicy<HttpResponseMessage> BuildCompositePolicy(string operationKey)
        {
            // Retry: 3 attempts with exponential backoff + small jitter (2s, 4s, 8s + 0-250ms)
            var rng = new Random();
            var retry = Policy
                .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                .Or<HttpRequestException>()
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: attempt =>
                        TimeSpan.FromSeconds(Math.Pow(2, attempt)) + TimeSpan.FromMilliseconds(rng.Next(0, 250)),
                    onRetry: (outcome, delay, attempt, ctx) =>
                        Log.Warning("[Polly-Retry] op={OperationKey} attempt={Attempt} delay={Delay}s status={Status}",
                            ctx.OperationKey ?? operationKey,
                            attempt,
                            Math.Round(delay.TotalSeconds, 3),
                            outcome.Result?.StatusCode)
                );

            // Circuit Breaker: open after 5 consecutive failures for 30s
            var breaker = Policy
                .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                .Or<HttpRequestException>()
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: 5,
                    durationOfBreak: TimeSpan.FromSeconds(30),
                    onBreak: (outcome, span) =>
                        Log.Warning("[CircuitBreaker] op={OperationKey} OPEN for {Break}s status={Status}",
                            operationKey, span.TotalSeconds, outcome.Result?.StatusCode),
                    onReset: () =>
                        Log.Information("[CircuitBreaker] op={OperationKey} RESET", operationKey),
                    onHalfOpen: () =>
                        Log.Information("[CircuitBreaker] op={OperationKey} HALF-OPEN", operationKey)
                );

            // Timeout: hard cap each outbound call at 2s (Optimistic cancels token)
            var timeout = Policy.TimeoutAsync<HttpResponseMessage>(
                timeout: TimeSpan.FromSeconds(20),
                timeoutStrategy: TimeoutStrategy.Optimistic,
                onTimeoutAsync: (context, span, task, ct) =>
                {
                    Log.Error("[Timeout] op={OperationKey} after {Seconds}s",
                        context.OperationKey ?? operationKey, span.TotalSeconds);
                    return Task.CompletedTask;
                });

            // Fallback: safe JSON [] for enrichment so deserialization succeeds
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

            // Bulkhead: limit parallel calls to dependency to avoid thread starvation
            var bulkhead = Policy.BulkheadAsync<HttpResponseMessage>(
                maxParallelization: 20,
                maxQueuingActions: 10,
                onBulkheadRejectedAsync: ctx =>
                {
                    Log.Warning("[Bulkhead] op={OperationKey} rejected due to overload",
                        ctx.OperationKey ?? operationKey);
                    return Task.CompletedTask;
                });

            // Wrap order (outer → inner): Bulkhead → Fallback → Breaker → Retry → Timeout
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
    }
}
