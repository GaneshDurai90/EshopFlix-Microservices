using System;

namespace CatalogService.Application.DTO
{
    public sealed class PriceHistoryEntryDto
    {
        public int PriceHistoryId { get; set; }
        public int? ProductId { get; set; }
        public int? SkuId { get; set; }
        public decimal? OldPrice { get; set; }
        public decimal NewPrice { get; set; }
        public string Currency { get; set; } = "USD";
        public string ChangedBy { get; set; } = string.Empty;
        public DateTime ChangedDate { get; set; }
    }
}
