using System.Threading;
using System.Threading.Tasks;
using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;
using CatalogService.Application.PriceHistory.Commands;
using CatalogService.Application.PriceHistory.Queries;
using CatalogService.Application.Services.Abstractions;

namespace CatalogService.Application.Services.Implementation
{
    public sealed class PriceHistoryAppService : IPriceHistoryAppService
    {
        private readonly IDispatcher _dispatcher;

        public PriceHistoryAppService(IDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public async Task<PagedResult<PriceHistoryEntryDto>> GetHistoryAsync(GetPriceHistoryQuery query, CancellationToken ct = default)
            => await _dispatcher.Query(query, ct);

        public async Task<PriceHistoryEntryDto> RecordAsync(RecordPriceChangeCommand command, CancellationToken ct = default)
            => await _dispatcher.Send(command, ct);
    }
}
