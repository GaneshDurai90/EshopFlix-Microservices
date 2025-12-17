using System;
using System.Collections.Generic;
using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;

namespace CatalogService.Application.Products.Queries
{
    public sealed class GetProductsByIdsQuery : IQuery<IEnumerable<ProductDTO>>
    {
        public IReadOnlyCollection<int> ProductIds { get; init; } = Array.Empty<int>();
    }
}
