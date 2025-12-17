using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;

namespace CatalogService.Application.ProductVariants.Commands
{
    public sealed class SetDefaultProductVariantCommand : ICommand<ProductVariantDto>
    {
        public int ProductId { get; init; }
        public int SkuId { get; init; }
    }
}
