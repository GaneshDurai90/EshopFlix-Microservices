using System.Collections.Generic;
using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;

namespace CatalogService.Application.ProductImages.Queries
{
    public sealed class GetProductImagesQuery : IQuery<IReadOnlyList<ProductImageDto>>
    {
        public int ProductId { get; init; }
    }
}
