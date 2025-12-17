using System;

namespace eShopFlix.Web.Models
{
    public sealed class PriceHistoryEntryModel
    {
        public int PriceHistoryId { get; init; }
        public int? ProductId { get; init; }
        public int? SkuId { get; init; }
        public decimal? OldPrice { get; init; }
        public decimal NewPrice { get; init; }
        public string Currency { get; init; } = "USD";
        public string ChangedBy { get; init; } = string.Empty;
        public DateTime ChangedDate { get; init; }
    }
}
