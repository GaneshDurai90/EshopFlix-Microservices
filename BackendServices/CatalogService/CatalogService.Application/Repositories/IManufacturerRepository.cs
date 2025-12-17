using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CatalogService.Domain.Entities;

namespace CatalogService.Application.Repositories
{
    public interface IManufacturerRepository
    {
        Task<Manufacturer?> GetByIdAsync(int manufacturerId, CancellationToken ct = default);
        Task<bool> ExistsByNameAsync(string name, int? excludeId = null, CancellationToken ct = default);
        Task<(IReadOnlyList<Manufacturer> Items, int TotalCount)> SearchAsync(string? term, int page, int pageSize, CancellationToken ct = default);
        Task AddAsync(Manufacturer manufacturer, CancellationToken ct = default);
        Task UpdateAsync(Manufacturer manufacturer, CancellationToken ct = default);
        Task DeleteAsync(Manufacturer manufacturer, CancellationToken ct = default);
    }
}
