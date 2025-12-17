using System.Collections.Generic;
using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;

namespace CatalogService.Application.ProductRelationships.Queries
{
    public sealed class GetProductRelationshipsQuery : IQuery<IReadOnlyList<ProductRelationshipDto>>
    {
        public int ParentProductId { get; init; }
        public byte? RelationshipType { get; init; }
    }
}
