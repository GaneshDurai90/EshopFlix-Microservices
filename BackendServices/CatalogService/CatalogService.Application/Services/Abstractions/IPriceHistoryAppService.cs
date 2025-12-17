using System.Threading;
using System.Threading.Tasks;
using CatalogService.Application.DTO;
using CatalogService.Application.PriceHistory.Commands;
using CatalogService.Application.PriceHistory.Queries;

namespace CatalogService.Application.Services.Abstractions
{
    public interface IPriceHistoryAppService
    {
        Task<PriceHistoryEntryDto> RecordAsync(RecordPriceChangeCommand command, CancellationToken ct = default);
        Task<PagedResult<PriceHistoryEntryDto>> GetHistoryAsync(GetPriceHistoryQuery query, CancellationToken ct = default);
    }
}
