namespace CatalogService.API.Contracts.ProductImages
{
    public sealed class CreateProductImageRequest
    {
        public int? SkuId { get; set; }
        public string Url { get; set; } = string.Empty;
        public string? AltText { get; set; }
        public int SortOrder { get; set; }
        public bool IsPrimary { get; set; }
    }
}
