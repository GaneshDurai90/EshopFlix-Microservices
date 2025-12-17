using Polly;
using Polly.Timeout;
using Serilog;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CatalogService.Api.Extensions
{
    /// <summary>
    /// Resilience pipeline for outbound HTTP calls. Matches CartService behaviour for retries, circuit breaking, timeouts, and fallbacks.
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

            var timeout = Policy.TimeoutAsync<HttpResponseMessage>(
                timeout: TimeSpan.FromSeconds(20),
                timeoutStrategy: TimeoutStrategy.Optimistic,
                onTimeoutAsync: (context, span, task, ct) =>
                {
                    Log.Error("[Timeout] op={OperationKey} after {Seconds}s",
                        context.OperationKey ?? operationKey, span.TotalSeconds);
                    return Task.CompletedTask;
                });

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

            var bulkhead = Policy.BulkheadAsync<HttpResponseMessage>(
                maxParallelization: 20,
                maxQueuingActions: 10,
                onBulkheadRejectedAsync: ctx =>
                {
                    Log.Warning("[Bulkhead] op={OperationKey} rejected due to overload",
                        ctx.OperationKey ?? operationKey);
                    return Task.CompletedTask;
                });

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
