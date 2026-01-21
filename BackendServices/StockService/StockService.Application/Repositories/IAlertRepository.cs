using StockService.Application.DTOs;
using StockService.Domain.Entities;

namespace StockService.Application.Repositories;

public interface IAlertRepository
{
    Task<StockAlert?> GetByIdAsync(Guid alertId, CancellationToken ct = default);
    Task<IEnumerable<StockAlert>> GetActiveAlertsAsync(CancellationToken ct = default);
    Task<IEnumerable<StockAlert>> GetByTypeAsync(string alertType, CancellationToken ct = default);
    Task<IEnumerable<StockAlert>> GetByStockItemAsync(Guid stockItemId, CancellationToken ct = default);
    Task<StockAlert> CreateAsync(StockAlert alert, CancellationToken ct = default);
    Task UpdateAsync(StockAlert alert, CancellationToken ct = default);
    Task<int> AcknowledgeAsync(Guid alertId, Guid acknowledgedBy, CancellationToken ct = default);
    Task<int> ResolveAsync(Guid alertId, CancellationToken ct = default);
}
