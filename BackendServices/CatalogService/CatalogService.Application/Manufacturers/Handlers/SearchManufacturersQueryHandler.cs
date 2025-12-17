using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;
using CatalogService.Application.Manufacturers.Queries;
using CatalogService.Application.Repositories;

namespace CatalogService.Application.Manufacturers.Handlers
{
    public sealed class SearchManufacturersQueryHandler : IQueryHandler<SearchManufacturersQuery, PagedResult<ManufacturerListItemDto>>
    {
        private readonly IManufacturerRepository _repository;
        private readonly IMapper _mapper;

        public SearchManufacturersQueryHandler(IManufacturerRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<PagedResult<ManufacturerListItemDto>> Handle(SearchManufacturersQuery query, CancellationToken ct)
        {
            var (items, total) = await _repository.SearchAsync(query.Term, query.Page, query.PageSize, ct);
            var mapped = _mapper.Map<IReadOnlyList<ManufacturerListItemDto>>(items);
            return new PagedResult<ManufacturerListItemDto>(mapped, total, query.Page, query.PageSize);
        }
    }
}
