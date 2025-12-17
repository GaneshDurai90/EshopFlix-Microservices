using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;

namespace CatalogService.Application.ProductVariants.Queries
{
    public sealed class GetProductVariantsByProductQuery : IQuery<IReadOnlyList<ProductVariantListItemDto>>
    {
        public int ProductId { get; init; }
    }
}
