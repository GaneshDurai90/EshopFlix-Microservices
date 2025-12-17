using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;

namespace CatalogService.Application.PriceHistory.Commands
{
    public sealed class RecordPriceChangeCommand : ICommand<PriceHistoryEntryDto>
    {
        public int? ProductId { get; init; }
        public int? SkuId { get; init; }
        public decimal? OldPrice { get; init; }
        public decimal NewPrice { get; init; }
        public string Currency { get; init; } = "USD";
        public string ChangedBy { get; init; } = string.Empty;
    }
}
