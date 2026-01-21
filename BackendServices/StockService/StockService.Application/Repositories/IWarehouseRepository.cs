using StockService.Application.DTOs;
using StockService.Domain.Entities;

namespace StockService.Application.Repositories;

public interface IWarehouseRepository
{
    Task<Warehouse?> GetByIdAsync(Guid warehouseId, CancellationToken ct = default);
    Task<Warehouse?> GetByCodeAsync(string warehouseCode, CancellationToken ct = default);
    Task<IEnumerable<Warehouse>> GetAllActiveAsync(CancellationToken ct = default);
    Task<IEnumerable<Warehouse>> GetByTypeAsync(string type, CancellationToken ct = default);
    Task<IEnumerable<Warehouse>> GetByPriorityOrderAsync(CancellationToken ct = default);
    Task<Warehouse> CreateAsync(Warehouse warehouse, CancellationToken ct = default);
    Task UpdateAsync(Warehouse warehouse, CancellationToken ct = default);
    Task<bool> SetActiveStatusAsync(Guid warehouseId, bool isActive, CancellationToken ct = default);
}
