using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;
using CatalogService.Application.PriceHistory.Queries;
using CatalogService.Application.Repositories;
using PriceHistoryEntity = CatalogService.Domain.Entities.PriceHistory;

namespace CatalogService.Application.PriceHistory.Handlers
{
    public sealed class GetPriceHistoryQueryHandler : IQueryHandler<GetPriceHistoryQuery, PagedResult<PriceHistoryEntryDto>>
    {
        private readonly IPriceHistoryRepository _repository;
        private readonly IMapper _mapper;

        public GetPriceHistoryQueryHandler(IPriceHistoryRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<PagedResult<PriceHistoryEntryDto>> Handle(GetPriceHistoryQuery query, CancellationToken ct)
        {
            var (items, total) = await _repository.GetHistoryAsync(query.ProductId, query.SkuId, query.Page, query.PageSize, ct);
            var mapped = _mapper.Map<IReadOnlyList<PriceHistoryEntryDto>>(items);
            return new PagedResult<PriceHistoryEntryDto>(mapped, total, query.Page, query.PageSize);
        }
    }
}
