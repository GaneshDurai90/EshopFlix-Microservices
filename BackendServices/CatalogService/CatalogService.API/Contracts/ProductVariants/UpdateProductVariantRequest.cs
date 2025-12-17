namespace CatalogService.API.Contracts.ProductVariants
{
    public sealed class UpdateProductVariantRequest
    {
        public string Sku { get; set; } = string.Empty;
        public string? Barcode { get; set; }
        public string? Attributes { get; set; }
        public decimal UnitPrice { get; set; }
        public string Currency { get; set; } = "USD";
        public decimal? CompareAtPrice { get; set; }
        public decimal? CostPrice { get; set; }
        public bool IsDefault { get; set; }
    }
}
