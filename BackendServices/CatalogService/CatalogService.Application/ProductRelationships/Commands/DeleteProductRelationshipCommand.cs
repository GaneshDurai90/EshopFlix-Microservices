using CatalogService.Application.CQRS;

namespace CatalogService.Application.ProductRelationships.Commands
{
    public sealed class DeleteProductRelationshipCommand : ICommand<bool>
    {
        public int ParentProductId { get; init; }
        public int RelatedProductId { get; init; }
    }
}
