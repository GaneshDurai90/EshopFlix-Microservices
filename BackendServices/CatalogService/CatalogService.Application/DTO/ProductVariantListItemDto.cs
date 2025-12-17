namespace CatalogService.Application.DTO
{
    public sealed class ProductVariantListItemDto
    {
        public int SkuId { get; set; }
        public string Sku { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public string Currency { get; set; } = "USD";
        public bool IsDefault { get; set; }
    }
}
