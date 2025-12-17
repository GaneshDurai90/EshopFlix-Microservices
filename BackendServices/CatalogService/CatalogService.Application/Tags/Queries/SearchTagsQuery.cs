using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;

namespace CatalogService.Application.Tags.Queries
{
    public sealed class SearchTagsQuery : IQuery<PagedResult<TagListItemDto>>
    {
        public string? Term { get; init; }
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 20;
    }
}
