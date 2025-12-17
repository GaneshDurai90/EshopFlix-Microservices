using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CatalogService.Application.DTO;
using CatalogService.Application.Manufacturers.Commands;
using CatalogService.Application.Manufacturers.Queries;

namespace CatalogService.Application.Services.Abstractions
{
    public interface IManufacturerAppService
    {
        Task<PagedResult<ManufacturerListItemDto>> SearchAsync(SearchManufacturersQuery query, CancellationToken ct = default);
        Task<ManufacturerDto?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<ManufacturerDto> CreateAsync(CreateManufacturerCommand command, CancellationToken ct = default);
        Task<ManufacturerDto> UpdateAsync(UpdateManufacturerCommand command, CancellationToken ct = default);
        Task DeleteAsync(DeleteManufacturerCommand command, CancellationToken ct = default);
    }
}
