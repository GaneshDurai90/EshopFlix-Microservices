using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;

namespace CatalogService.Application.Products.Commands
{
    public sealed class CreateProductCommand : ICommand<ProductDetailDto>
    {
        public string Name { get; init; } = string.Empty;
        public string Slug { get; init; } = string.Empty;
        public string? ShortDescription { get; init; }
        public string? LongDescription { get; init; }
        public int? BrandId { get; init; }
        public int? ManufacturerId { get; init; }
        public int? CategoryId { get; init; }
        public bool IsSearchable { get; init; } = true;
        public decimal? Weight { get; init; }
        public string? Dimensions { get; init; }
        public string? PrimaryImageUrl { get; init; }
        public string? SeoTitle { get; init; }
        public string? SeoDescription { get; init; }
        public string? SeoKeywords { get; init; }
        public byte Status { get; init; }
    }
}
