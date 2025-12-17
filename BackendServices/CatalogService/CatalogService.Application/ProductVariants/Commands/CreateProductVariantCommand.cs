using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;

namespace CatalogService.Application.ProductVariants.Commands
{
    public sealed class CreateProductVariantCommand : ICommand<ProductVariantDto>
    {
        public int ProductId { get; init; }
        public string Sku { get; init; } = string.Empty;
        public string? Barcode { get; init; }
        public string? Attributes { get; init; }
        public decimal UnitPrice { get; init; }
        public string Currency { get; init; } = "USD";
        public decimal? CompareAtPrice { get; init; }
        public decimal? CostPrice { get; init; }
        public bool IsDefault { get; init; }
    }
}
