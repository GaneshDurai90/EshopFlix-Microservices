using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;
using CatalogService.Application.Promotions.Queries;
using CatalogService.Application.Repositories;

namespace CatalogService.Application.Promotions.Handlers
{
    public sealed class GetPromotionByIdQueryHandler : IQueryHandler<GetPromotionByIdQuery, PromotionDto?>
    {
        private readonly IPromotionRepository _promotionRepository;
        private readonly IMapper _mapper;

        public GetPromotionByIdQueryHandler(IPromotionRepository promotionRepository, IMapper mapper)
        {
            _promotionRepository = promotionRepository;
            _mapper = mapper;
        }

        public async Task<PromotionDto?> Handle(GetPromotionByIdQuery query, CancellationToken ct)
        {
            var promotion = await _promotionRepository.GetByIdAsync(query.PromotionId, ct);
            if (promotion == null)
            {
                return null;
            }

            var dto = _mapper.Map<PromotionDto>(promotion);
            dto.ProductIds = promotion.Products?.Select(p => p.ProductId).ToArray() ?? System.Array.Empty<int>();
            return dto;
        }
    }
}
