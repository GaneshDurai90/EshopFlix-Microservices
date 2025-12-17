using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;
using CatalogService.Application.ProductRelationships.Commands;
using CatalogService.Application.Repositories;
using CatalogService.Application.Exceptions;

namespace CatalogService.Application.ProductRelationships.Handlers
{
    public sealed class UpdateProductRelationshipCommandHandler : ICommandHandler<UpdateProductRelationshipCommand, ProductRelationshipDto>
    {
        private readonly IProductRelationshipRepository _relationshipRepository;
        private readonly IMapper _mapper;

        public UpdateProductRelationshipCommandHandler(IProductRelationshipRepository relationshipRepository, IMapper mapper)
        {
            _relationshipRepository = relationshipRepository;
            _mapper = mapper;
        }

        public async Task<ProductRelationshipDto> Handle(UpdateProductRelationshipCommand command, CancellationToken ct)
        {
            var relationships = await _relationshipRepository.GetByParentAsync(command.ParentProductId, null, ct);
            var relationship = relationships.FirstOrDefault(r => r.RelatedProductId == command.RelatedProductId);
            if (relationship == null)
            {
                throw AppException.NotFound("relationship", "Relationship not found");
            }

            relationship.RelationshipType = command.RelationshipType;
            relationship.SortOrder = command.SortOrder;

            await _relationshipRepository.UpdateAsync(relationship, ct);
            return _mapper.Map<ProductRelationshipDto>(relationship);
        }
    }
}
