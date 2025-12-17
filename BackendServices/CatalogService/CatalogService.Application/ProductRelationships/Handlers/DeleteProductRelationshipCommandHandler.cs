using System.Threading;
using System.Threading.Tasks;
using CatalogService.Application.CQRS;
using CatalogService.Application.ProductRelationships.Commands;
using CatalogService.Application.Repositories;

namespace CatalogService.Application.ProductRelationships.Handlers
{
    public sealed class DeleteProductRelationshipCommandHandler : ICommandHandler<DeleteProductRelationshipCommand, bool>
    {
        private readonly IProductRelationshipRepository _relationshipRepository;

        public DeleteProductRelationshipCommandHandler(IProductRelationshipRepository relationshipRepository)
        {
            _relationshipRepository = relationshipRepository;
        }

        public async Task<bool> Handle(DeleteProductRelationshipCommand command, CancellationToken ct)
        {
            await _relationshipRepository.DeleteAsync(command.ParentProductId, command.RelatedProductId, ct);
            return true;
        }
    }
}
