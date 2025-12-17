using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;

namespace CatalogService.Application.ProductRelationships.Commands
{
    public sealed class UpdateProductRelationshipCommand : ICommand<ProductRelationshipDto>
    {
        public int ParentProductId { get; init; }
        public int RelatedProductId { get; init; }
        public byte RelationshipType { get; init; }
        public int SortOrder { get; init; }
    }
}
