using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;
using CatalogService.Application.Promotions.Queries;
using CatalogService.Application.Repositories;

namespace CatalogService.Application.Promotions.Handlers
{
    public sealed class SearchPromotionsQueryHandler : IQueryHandler<SearchPromotionsQuery, PagedResult<PromotionListItemDto>>
    {
        private readonly IPromotionRepository _promotionRepository;
        private readonly IMapper _mapper;

        public SearchPromotionsQueryHandler(IPromotionRepository promotionRepository, IMapper mapper)
        {
            _promotionRepository = promotionRepository;
            _mapper = mapper;
        }

        public async Task<PagedResult<PromotionListItemDto>> Handle(SearchPromotionsQuery query, CancellationToken ct)
        {
            var (items, total) = await _promotionRepository.SearchAsync(query.Term, query.IsActive, query.Page, query.PageSize, ct);
            var mapped = _mapper.Map<IReadOnlyList<PromotionListItemDto>>(items);
            return new PagedResult<PromotionListItemDto>(mapped, total, query.Page, query.PageSize);
        }
    }
}
