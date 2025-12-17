using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;

namespace CatalogService.Application.Promotions.Queries
{
    public sealed class SearchPromotionsQuery : IQuery<PagedResult<PromotionListItemDto>>
    {
        public string? Term { get; init; }
        public bool? IsActive { get; init; }
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 20;
    }
}
