using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;

namespace CatalogService.Application.ProductVariants.Queries
{
    public sealed class GetProductVariantByIdQuery : IQuery<ProductVariantDto?>
    {
        public int SkuId { get; init; }
    }
}
