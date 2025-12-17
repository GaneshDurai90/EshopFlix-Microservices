using System.Threading;
using System.Threading.Tasks;
using CatalogService.Application.DTO;
using CatalogService.Application.Promotions.Commands;
using CatalogService.Application.Promotions.Queries;

namespace CatalogService.Application.Services.Abstractions
{
    public interface IPromotionAppService
    {
        Task<PromotionDto> CreateAsync(CreatePromotionCommand command, CancellationToken ct = default);
        Task<PromotionDto> UpdateAsync(UpdatePromotionCommand command, CancellationToken ct = default);
        Task DeleteAsync(DeletePromotionCommand command, CancellationToken ct = default);
        Task<PromotionDto?> GetByIdAsync(int promotionId, CancellationToken ct = default);
        Task<PagedResult<PromotionListItemDto>> SearchAsync(SearchPromotionsQuery query, CancellationToken ct = default);
    }
}
