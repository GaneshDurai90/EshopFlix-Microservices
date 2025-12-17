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
    public sealed class GetAllTagsQueryHandler : IQueryHandler<GetAllTagsQuery, IReadOnlyList<TagDto>>
    {
        private readonly ITagRepository _tagRepository;
        private readonly IMapper _mapper;

        public GetAllTagsQueryHandler(ITagRepository tagRepository, IMapper mapper)
        {
            _tagRepository = tagRepository;
            _mapper = mapper;
        }

        public async Task<IReadOnlyList<TagDto>> Handle(GetAllTagsQuery query, CancellationToken ct)
        {
            var tags = await _tagRepository.GetAllAsync(ct);
            return _mapper.Map<IReadOnlyList<TagDto>>(tags);
        }
    }
}
