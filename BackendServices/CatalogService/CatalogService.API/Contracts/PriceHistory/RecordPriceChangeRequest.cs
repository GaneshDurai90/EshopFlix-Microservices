namespace CatalogService.API.Contracts.PriceHistory
{
    public sealed class RecordPriceChangeRequest
    {
        public int? ProductId { get; set; }
        public int? SkuId { get; set; }
        public decimal? OldPrice { get; set; }
        public decimal NewPrice { get; set; }
        public string Currency { get; set; } = "USD";
        public string ChangedBy { get; set; } = string.Empty;
    }
}
