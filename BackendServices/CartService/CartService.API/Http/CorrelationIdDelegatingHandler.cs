namespace CartService.Api.Http
{
    public sealed class CorrelationIdDelegatingHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _contextAccessor;
        public CorrelationIdDelegatingHandler(IHttpContextAccessor accessor) => _contextAccessor = accessor;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var ctx = _contextAccessor.HttpContext;
            if (ctx != null && ctx.Items.TryGetValue("x-correlation-id", out var cidObj) && cidObj is string cid && !string.IsNullOrWhiteSpace(cid))
            {
                if (!request.Headers.Contains("x-correlation-id"))
                    request.Headers.Add("x-correlation-id", cid);
            }
            return base.SendAsync(request, cancellationToken);
        }
    }
}
