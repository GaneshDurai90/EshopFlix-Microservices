using CatalogService.Application.CQRS;

namespace CatalogService.Application.ProductVariants.Commands
{
    public sealed class DeleteProductVariantCommand : ICommand<bool>
    {
        public int SkuId { get; init; }
    }
}
