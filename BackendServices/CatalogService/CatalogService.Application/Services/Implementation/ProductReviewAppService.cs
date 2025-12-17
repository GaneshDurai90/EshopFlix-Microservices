using System.Threading;
using System.Threading.Tasks;
using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;
using CatalogService.Application.ProductReviews.Commands;
using CatalogService.Application.ProductReviews.Queries;
using CatalogService.Application.Services.Abstractions;

namespace CatalogService.Application.Services.Implementation
{
    public sealed class ProductReviewAppService : IProductReviewAppService
    {
        private readonly IDispatcher _dispatcher;

        public ProductReviewAppService(IDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public async Task<ProductReviewDto> CreateAsync(CreateProductReviewCommand command, CancellationToken ct = default)
            => await _dispatcher.Send(command, ct);

        public async Task DeleteAsync(DeleteProductReviewCommand command, CancellationToken ct = default)
        {
            await _dispatcher.Send(command, ct);
        }

        public async Task<PagedResult<ProductReviewListItemDto>> SearchAsync(SearchProductReviewsQuery query, CancellationToken ct = default)
            => await _dispatcher.Query(query, ct);

        public async Task<ProductReviewDto> UpdateAsync(UpdateProductReviewCommand command, CancellationToken ct = default)
            => await _dispatcher.Send(command, ct);
    }
}
