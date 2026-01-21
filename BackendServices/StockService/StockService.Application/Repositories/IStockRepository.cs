using StockService.Application.DTOs;
using StockService.Domain.Entities;

namespace StockService.Application.Repositories;

public interface IStockRepository
{
    // Stock Item Operations
    Task<StockItem?> GetByIdAsync(Guid stockItemId, CancellationToken ct = default);
    Task<StockItem?> GetByProductAndWarehouseAsync(Guid productId, Guid? variationId, Guid warehouseId, CancellationToken ct = default);
    Task<IEnumerable<StockItem>> GetByProductAsync(Guid productId, Guid? variationId = null, CancellationToken ct = default);
    Task<IEnumerable<StockItem>> GetByWarehouseAsync(Guid warehouseId, CancellationToken ct = default);
    Task<IEnumerable<StockItem>> GetLowStockItemsAsync(CancellationToken ct = default);
    Task<IEnumerable<StockItem>> GetExpiringSoonAsync(int daysThreshold = 30, CancellationToken ct = default);
    Task<StockItem> CreateAsync(StockItem stockItem, CancellationToken ct = default);
    Task UpdateAsync(StockItem stockItem, CancellationToken ct = default);
    
    // Stock Operations via Stored Procedures
    Task<int> IncreaseStockAsync(Guid stockItemId, int quantity, string reason, Guid performedBy, CancellationToken ct = default);
    Task<int> DecreaseStockAsync(Guid stockItemId, int quantity, string reason, Guid performedBy, CancellationToken ct = default);
    Task<int> AdjustStockAsync(Guid stockItemId, int adjustmentQuantity, string reason, Guid performedBy, Guid? approvedBy, CancellationToken ct = default);
    
    // Reservation Operations
    Task<int> ReserveStockAsync(Guid stockItemId, Guid reservationId, int quantity, CancellationToken ct = default);
    Task<int> CommitReservationAsync(Guid reservationId, CancellationToken ct = default);
    Task<int> ReleaseExpiredReservationsAsync(CancellationToken ct = default);
    
    // Alerts
    Task<int> TriggerLowStockAlertsAsync(CancellationToken ct = default);
    Task<int> TriggerOverStockAlertsAsync(CancellationToken ct = default);
    
    // Batch Operations
    Task<int> ExpireStockBatchesAsync(CancellationToken ct = default);
    Task<int> RecalculateSafetyStockAsync(CancellationToken ct = default);
    
    // Availability Check
    Task<CheckAvailabilityResponse> CheckAvailabilityAsync(CheckAvailabilityRequest request, CancellationToken ct = default);
    Task<StockAvailabilityDTO?> GetAvailabilityAsync(Guid productId, Guid? variationId, CancellationToken ct = default);
}
