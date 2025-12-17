using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;

namespace CatalogService.Application.Products.Queries
{
    public sealed class GetProductByIdQuery : IQuery<ProductDetailDto?>
    {
        public int ProductId { get; init; }
    }
}
