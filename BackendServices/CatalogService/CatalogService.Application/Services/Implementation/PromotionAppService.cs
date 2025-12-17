using System.Threading;
using System.Threading.Tasks;
using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;
using CatalogService.Application.Promotions.Commands;
using CatalogService.Application.Promotions.Queries;
using CatalogService.Application.Services.Abstractions;

namespace CatalogService.Application.Services.Implementation
{
    public sealed class PromotionAppService : IPromotionAppService
    {
        private readonly IDispatcher _dispatcher;

        public PromotionAppService(IDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public async Task<PromotionDto> CreateAsync(CreatePromotionCommand command, CancellationToken ct = default)
            => await _dispatcher.Send(command, ct);

        public async Task DeleteAsync(DeletePromotionCommand command, CancellationToken ct = default)
        {
            await _dispatcher.Send(command, ct);
        }

        public async Task<PromotionDto?> GetByIdAsync(int promotionId, CancellationToken ct = default)
            => await _dispatcher.Query(new GetPromotionByIdQuery { PromotionId = promotionId }, ct);

        public async Task<PagedResult<PromotionListItemDto>> SearchAsync(SearchPromotionsQuery query, CancellationToken ct = default)
            => await _dispatcher.Query(query, ct);

        public async Task<PromotionDto> UpdateAsync(UpdatePromotionCommand command, CancellationToken ct = default)
            => await _dispatcher.Send(command, ct);
    }
}
