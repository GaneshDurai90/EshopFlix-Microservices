namespace CatalogService.API.Contracts.ProductImages
{
    public sealed class UpdateProductImageRequest
    {
        public string Url { get; set; } = string.Empty;
        public string? AltText { get; set; }
        public int SortOrder { get; set; }
        public bool IsPrimary { get; set; }
    }
}
