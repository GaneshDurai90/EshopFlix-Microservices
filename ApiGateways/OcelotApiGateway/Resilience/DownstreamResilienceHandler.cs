using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using Polly.Fallback;
using Serilog;
using System.Net;

namespace OcelotApiGateway.Resilience
{
    /// <summary>
    /// DelegatingHandler applying resilience policies for downstream calls.
    /// Logs standardized events: [Timeout], [Polly-Retry], [CircuitBreaker], [Fallback].
    /// Attach via ocelot json: "DelegatingHandlers": ["DownstreamResilienceHandler"].
    /// </summary>
    public sealed class DownstreamResilienceHandler : DelegatingHandler
    {
        private readonly IAsyncPolicy<HttpResponseMessage> _policy;
        public DownstreamResilienceHandler() => _policy = BuildPolicy();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var op = InferOperationKey(request);
            var context = new Context(op); // Polly v8 Context
            return _policy.ExecuteAsync((ctx, ct) => base.SendAsync(request, ct), context, cancellationToken);
        }

        private static string InferOperationKey(HttpRequestMessage req)
        {
            var path = req.RequestUri?.AbsolutePath ?? "/";
            var segments = path.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            // expecting /api/{service}/...
            var svc = segments.Length > 1 ? segments[1] : segments.FirstOrDefault() ?? "downstream";
            return $"gateway:{svc}";
        }

        private static IAsyncPolicy<HttpResponseMessage> BuildPolicy()
        {
            var timeout = Policy.TimeoutAsync<HttpResponseMessage>(
                TimeSpan.FromSeconds(2), TimeoutStrategy.Pessimistic,
                onTimeoutAsync: (ctx, span, task) =>
                {
                    Log.Error("[Timeout] op={OperationKey} after {Seconds}s", ctx.OperationKey, span.TotalSeconds);
                    return Task.CompletedTask;
                });

            var retry = Policy
                .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                .Or<HttpRequestException>()
                .WaitAndRetryAsync(3,
                    attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                    (outcome, delay, attempt, ctx) =>
                        Log.Warning("[Polly-Retry] op={OperationKey} attempt={Attempt} delay={Delay}s status={Status}",
                            ctx.OperationKey, attempt, delay.TotalSeconds, outcome.Result?.StatusCode));

            var breaker = Policy
                .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                .Or<HttpRequestException>()
                .CircuitBreakerAsync(3, TimeSpan.FromSeconds(30),
                    onBreak: (outcome, span, ctx) =>
                        Log.Warning("[CircuitBreaker] op={OperationKey} OPEN for {Break}s status={Status}",
                            ctx.OperationKey, span.TotalSeconds, outcome.Result?.StatusCode),
                    onReset: ctx => Log.Information("[CircuitBreaker] op={OperationKey} RESET", ctx.OperationKey),
                    onHalfOpen: () => Log.Information("[CircuitBreaker] HALF-OPEN"));

            var fallback = Policy<HttpResponseMessage>
                .Handle<Exception>()
                .OrResult(r => !r.IsSuccessStatusCode)
                .FallbackAsync(ct =>
                {
                    Log.Warning("[Fallback] op=gateway returning problem details response");
                    var body = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        title = "Upstream service unavailable",
                        status = 502,
                        type = "urn:problem:gateway:upstream-failure",
                        detail = "The API Gateway could not reach the downstream service. Please try again later."
                    });
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadGateway)
                    {
                        Content = new StringContent(body, System.Text.Encoding.UTF8, "application/problem+json")
                    });
                });

            // Fallback -> Breaker -> Retry -> Timeout
            return Policy.WrapAsync(fallback, Policy.WrapAsync(breaker, Policy.WrapAsync(retry, timeout)));
        }
    }
}
