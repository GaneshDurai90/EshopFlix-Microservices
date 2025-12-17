using System.Threading;
using System.Threading.Tasks;
using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;
using CatalogService.Application.Manufacturers.Commands;
using CatalogService.Application.Manufacturers.Queries;
using CatalogService.Application.Services.Abstractions;

namespace CatalogService.Application.Services.Implementation
{
    public sealed class ManufacturerAppService : IManufacturerAppService
    {
        private readonly IDispatcher _dispatcher;

        public ManufacturerAppService(IDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public async Task<ManufacturerDto> CreateAsync(CreateManufacturerCommand command, CancellationToken ct = default)
            => await _dispatcher.Send(command, ct);

        public async Task DeleteAsync(DeleteManufacturerCommand command, CancellationToken ct = default)
        {
            await _dispatcher.Send(command, ct);
        }

        public async Task<ManufacturerDto?> GetByIdAsync(int id, CancellationToken ct = default)
            => await _dispatcher.Query(new GetManufacturerByIdQuery { ManufacturerId = id }, ct);

        public async Task<PagedResult<ManufacturerListItemDto>> SearchAsync(SearchManufacturersQuery query, CancellationToken ct = default)
            => await _dispatcher.Query(query, ct);

        public async Task<ManufacturerDto> UpdateAsync(UpdateManufacturerCommand command, CancellationToken ct = default)
            => await _dispatcher.Send(command, ct);
    }
}
