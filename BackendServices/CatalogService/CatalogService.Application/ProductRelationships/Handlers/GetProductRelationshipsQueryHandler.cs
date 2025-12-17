using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;
using CatalogService.Application.ProductRelationships.Queries;
using CatalogService.Application.Repositories;

namespace CatalogService.Application.ProductRelationships.Handlers
{
    public sealed class GetProductRelationshipsQueryHandler : IQueryHandler<GetProductRelationshipsQuery, IReadOnlyList<ProductRelationshipDto>>
    {
        private readonly IProductRelationshipRepository _relationshipRepository;
        private readonly IMapper _mapper;

        public GetProductRelationshipsQueryHandler(IProductRelationshipRepository relationshipRepository, IMapper mapper)
        {
            _relationshipRepository = relationshipRepository;
            _mapper = mapper;
        }

        public async Task<IReadOnlyList<ProductRelationshipDto>> Handle(GetProductRelationshipsQuery query, CancellationToken ct)
        {
            var relationships = await _relationshipRepository.GetByParentAsync(query.ParentProductId, query.RelationshipType, ct);
            return _mapper.Map<IReadOnlyList<ProductRelationshipDto>>(relationships);
        }
    }
}
