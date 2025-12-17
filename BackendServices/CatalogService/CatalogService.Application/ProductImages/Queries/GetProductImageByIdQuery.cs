using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;

namespace CatalogService.Application.ProductImages.Queries
{
    public sealed class GetProductImageByIdQuery : IQuery<ProductImageDto?>
    {
        public int ProductImageId { get; init; }
    }
}
