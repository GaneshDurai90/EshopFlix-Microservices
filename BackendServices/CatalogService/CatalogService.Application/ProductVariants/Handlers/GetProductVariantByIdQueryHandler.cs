using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;
using CatalogService.Application.ProductVariants.Queries;
using CatalogService.Application.Repositories;

namespace CatalogService.Application.ProductVariants.Handlers
{
    public sealed class GetProductVariantByIdQueryHandler : IQueryHandler<GetProductVariantByIdQuery, ProductVariantDto?>
    {
        private readonly IProductVariantRepository _variantRepository;
        private readonly IMapper _mapper;

        public GetProductVariantByIdQueryHandler(IProductVariantRepository variantRepository, IMapper mapper)
        {
            _variantRepository = variantRepository;
            _mapper = mapper;
        }

        public async Task<ProductVariantDto?> Handle(GetProductVariantByIdQuery query, CancellationToken ct)
        {
            var variant = await _variantRepository.GetByIdAsync(query.SkuId, ct);
            return variant == null ? null : _mapper.Map<ProductVariantDto>(variant);
        }
    }
}
