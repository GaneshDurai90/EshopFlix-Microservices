using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;
using CatalogService.Application.Exceptions;
using CatalogService.Application.ProductRelationships.Commands;
using CatalogService.Application.Repositories;
using CatalogService.Domain.Entities;

namespace CatalogService.Application.ProductRelationships.Handlers
{
    public sealed class AddProductRelationshipCommandHandler : ICommandHandler<AddProductRelationshipCommand, ProductRelationshipDto>
    {
        private readonly IProductRepository _productRepository;
        private readonly IProductRelationshipRepository _relationshipRepository;
        private readonly IMapper _mapper;

        public AddProductRelationshipCommandHandler(
            IProductRepository productRepository,
            IProductRelationshipRepository relationshipRepository,
            IMapper mapper)
        {
            _productRepository = productRepository;
            _relationshipRepository = relationshipRepository;
            _mapper = mapper;
        }

        public async Task<ProductRelationshipDto> Handle(AddProductRelationshipCommand command, CancellationToken ct)
        {
            var parent = await _productRepository.GetByIdAsync(command.ParentProductId, ct);
            if (parent == null)
            {
                throw AppException.NotFound("product", $"Parent product {command.ParentProductId} not found");
            }

            var related = await _productRepository.GetByIdAsync(command.RelatedProductId, ct);
            if (related == null)
            {
                throw AppException.NotFound("product", $"Related product {command.RelatedProductId} not found");
            }

            var relationship = new ProductRelationship
            {
                ParentProductId = command.ParentProductId,
                RelatedProductId = command.RelatedProductId,
                RelationshipType = command.RelationshipType,
                SortOrder = command.SortOrder
            };

            await _relationshipRepository.AddAsync(relationship, ct);
            return _mapper.Map<ProductRelationshipDto>(relationship);
        }
    }
}
