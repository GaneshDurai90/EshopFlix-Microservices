using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;
using CatalogService.Application.Tags.Commands;
using CatalogService.Application.Tags.Queries;
using CatalogService.Application.Services.Abstractions;

namespace CatalogService.Application.Services.Implementation
{
    public sealed class TagAppService : ITagAppService
    {
        private readonly IDispatcher _dispatcher;

        public TagAppService(IDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public async Task<TagDto> CreateAsync(CreateTagCommand command, CancellationToken ct = default)
            => await _dispatcher.Send(command, ct);

        public async Task DeleteAsync(DeleteTagCommand command, CancellationToken ct = default)
        {
            await _dispatcher.Send(command, ct);
        }

        public async Task<ProductDetailDto> AssignToProductAsync(AssignTagsToProductCommand command, CancellationToken ct = default)
            => await _dispatcher.Send(command, ct);

        public async Task<IReadOnlyList<TagDto>> GetAllAsync(CancellationToken ct = default)
            => await _dispatcher.Query(new GetAllTagsQuery(), ct);

        public async Task<PagedResult<TagListItemDto>> SearchAsync(SearchTagsQuery query, CancellationToken ct = default)
            => await _dispatcher.Query(query, ct);

        public async Task<TagDto> UpdateAsync(UpdateTagCommand command, CancellationToken ct = default)
            => await _dispatcher.Send(command, ct);
    }
}
