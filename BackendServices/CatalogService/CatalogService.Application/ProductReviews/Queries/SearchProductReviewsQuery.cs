using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;

namespace CatalogService.Application.ProductReviews.Queries
{
    public sealed class SearchProductReviewsQuery : IQuery<PagedResult<ProductReviewListItemDto>>
    {
        public int ProductId { get; init; }
        public bool? IsPublished { get; init; }
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 20;
    }
}
