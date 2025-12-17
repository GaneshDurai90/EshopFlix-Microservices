using CatalogService.Domain.Enums;

namespace CatalogService.API.Contracts.Products
{
    public sealed class UpdateProductRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? ShortDescription { get; set; }
        public string? LongDescription { get; set; }
        public int? BrandId { get; set; }
        public int? ManufacturerId { get; set; }
        public int? CategoryId { get; set; }
        public bool IsSearchable { get; set; } = true;
        public decimal? Weight { get; set; }
        public string? Dimensions { get; set; }
        public string? PrimaryImageUrl { get; set; }
        public string? SeoTitle { get; set; }
        public string? SeoDescription { get; set; }
        public string? SeoKeywords { get; set; }
        public ProductStatus Status { get; set; } = ProductStatus.Draft;
    }
}
