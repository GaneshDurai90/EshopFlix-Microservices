using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;
using CatalogService.Application.ProductVariants.Queries;
using CatalogService.Application.Repositories;

namespace CatalogService.Application.ProductVariants.Handlers
{
    public sealed class GetProductVariantsByProductQueryHandler : IQueryHandler<GetProductVariantsByProductQuery, IReadOnlyList<ProductVariantListItemDto>>
    {
        private readonly IProductVariantRepository _variantRepository;
        private readonly IMapper _mapper;

        public GetProductVariantsByProductQueryHandler(IProductVariantRepository variantRepository, IMapper mapper)
        {
            _variantRepository = variantRepository;
            _mapper = mapper;
        }

        public async Task<IReadOnlyList<ProductVariantListItemDto>> Handle(GetProductVariantsByProductQuery query, CancellationToken ct)
        {
            var variants = await _variantRepository.GetByProductAsync(query.ProductId, ct);
            return _mapper.Map<IReadOnlyList<ProductVariantListItemDto>>(variants);
        }
    }
}
