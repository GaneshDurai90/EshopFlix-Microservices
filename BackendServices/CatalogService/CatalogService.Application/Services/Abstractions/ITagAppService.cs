using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CatalogService.Application.DTO;
using CatalogService.Application.Tags.Commands;
using CatalogService.Application.Tags.Queries;

namespace CatalogService.Application.Services.Abstractions
{
    public interface ITagAppService
    {
        Task<TagDto> CreateAsync(CreateTagCommand command, CancellationToken ct = default);
        Task<TagDto> UpdateAsync(UpdateTagCommand command, CancellationToken ct = default);
        Task DeleteAsync(DeleteTagCommand command, CancellationToken ct = default);
        Task<ProductDetailDto> AssignToProductAsync(AssignTagsToProductCommand command, CancellationToken ct = default);
        Task<IReadOnlyList<TagDto>> GetAllAsync(CancellationToken ct = default);
        Task<PagedResult<TagListItemDto>> SearchAsync(SearchTagsQuery query, CancellationToken ct = default);
    }
}
