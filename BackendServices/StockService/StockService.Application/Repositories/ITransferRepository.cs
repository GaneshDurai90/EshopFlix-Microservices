using StockService.Application.DTOs;
using StockService.Domain.Entities;

namespace StockService.Application.Repositories;

public interface ITransferRepository
{
    Task<StockTransfer?> GetByIdAsync(Guid transferId, CancellationToken ct = default);
    Task<StockTransfer?> GetByIdWithItemsAsync(Guid transferId, CancellationToken ct = default);
    Task<IEnumerable<StockTransfer>> GetByWarehouseAsync(Guid warehouseId, bool isSource, CancellationToken ct = default);
    Task<IEnumerable<StockTransfer>> GetByStatusAsync(string status, CancellationToken ct = default);
    Task<IEnumerable<StockTransfer>> GetInTransitAsync(CancellationToken ct = default);
    Task<Guid> CreateAsync(Guid fromWarehouseId, Guid toWarehouseId, Guid requestedBy, string? notes, CancellationToken ct = default);
    Task AddTransferItemAsync(StockTransferItem item, CancellationToken ct = default);
    Task UpdateAsync(StockTransfer transfer, CancellationToken ct = default);
    Task<int> CompleteTransferAsync(Guid transferId, CancellationToken ct = default);
}
