using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;
using CatalogService.Domain.Enums;

namespace CatalogService.Application.Products.Queries
{
    public sealed class SearchProductsQuery : IQuery<PagedResult<ProductListItemDto>>
    {
        public string? Term { get; init; }
        public int? CategoryId { get; init; }
        public ProductStatus? Status { get; init; }
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 20;
    }
}
