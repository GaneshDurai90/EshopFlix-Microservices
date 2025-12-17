using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;
using CatalogService.Application.Repositories;
using CatalogService.Application.Tags.Queries;

namespace CatalogService.Application.Tags.Handlers
{
    public sealed class SearchTagsQueryHandler : IQueryHandler<SearchTagsQuery, PagedResult<TagListItemDto>>
    {
        private readonly ITagRepository _tagRepository;
        private readonly IMapper _mapper;

        public SearchTagsQueryHandler(ITagRepository tagRepository, IMapper mapper)
        {
            _tagRepository = tagRepository;
            _mapper = mapper;
        }

        public async Task<PagedResult<TagListItemDto>> Handle(SearchTagsQuery query, CancellationToken ct)
        {
            var (items, total) = await _tagRepository.SearchAsync(query.Term, query.Page, query.PageSize, ct);
            var mapped = _mapper.Map<IReadOnlyList<TagListItemDto>>(items);
            return new PagedResult<TagListItemDto>(mapped, total, query.Page, query.PageSize);
        }
    }
}
