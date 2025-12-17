using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;

namespace CatalogService.Application.ProductImages.Commands
{
    public sealed class UpdateProductImageCommand : ICommand<ProductImageDto>
    {
        public int ProductImageId { get; init; }
        public string Url { get; init; } = string.Empty;
        public string? AltText { get; init; }
        public int SortOrder { get; init; }
        public bool IsPrimary { get; init; }
    }
}
