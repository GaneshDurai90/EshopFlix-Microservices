using Microsoft.AspNetCore.Http;
using System.Net.Http;
using System.Threading;

using CatalogService.API.Middleware;

namespace CatalogService.Api.Http
{
    public sealed class CorrelationIdDelegatingHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _contextAccessor;

        public CorrelationIdDelegatingHandler(IHttpContextAccessor contextAccessor)
            => _contextAccessor = contextAccessor;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var ctx = _contextAccessor.HttpContext;
            if (ctx != null && ctx.Items.TryGetValue(CorrelationIdMiddleware.HeaderName, out var value) && value is string correlationId)
            {
                if (!request.Headers.Contains(CorrelationIdMiddleware.HeaderName))
                {
                    request.Headers.Add(CorrelationIdMiddleware.HeaderName, correlationId);
                }
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}
