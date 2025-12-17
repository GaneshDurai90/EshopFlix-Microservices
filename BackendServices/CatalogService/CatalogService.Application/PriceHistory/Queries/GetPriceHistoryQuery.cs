using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;

namespace CatalogService.Application.PriceHistory.Queries
{
    public sealed class GetPriceHistoryQuery : IQuery<PagedResult<PriceHistoryEntryDto>>
    {
        public int? ProductId { get; init; }
        public int? SkuId { get; init; }
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 50;
    }
}
