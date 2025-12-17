namespace eShopFlix.Web.Models
{
    public sealed class ProductVariantModel
    {
        public int SkuId { get; init; }
        public string Sku { get; init; } = string.Empty;
        public decimal UnitPrice { get; init; }
        public string Currency { get; init; } = "USD";
        public bool IsDefault { get; init; }
    }
}
