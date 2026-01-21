using StockService.Application.DTOs;

namespace StockService.Application.Repositories;

public interface IReportRepository
{
    // Stock Valuation
    Task<IEnumerable<StockValuationDTO>> GetStockValuationAsync(Guid? warehouseId = null, CancellationToken ct = default);
    Task<decimal> GetTotalStockValueAsync(Guid? warehouseId = null, CancellationToken ct = default);
    
    // Stock Aging
    Task<IEnumerable<StockAgingSummaryDTO>> GetStockAgingSummaryAsync(CancellationToken ct = default);
    
    // Inventory Turnover
    Task<IEnumerable<InventoryTurnoverDTO>> GetInventoryTurnoverAsync(CancellationToken ct = default);
    
    // Dead Stock
    Task<IEnumerable<DeadStockDTO>> GetDeadStockAsync(int daysSinceLastMovement = 90, CancellationToken ct = default);
    
    // Low Stock Report
    Task<IEnumerable<LowStockReportDTO>> GetLowStockReportAsync(CancellationToken ct = default);
    
    // Expiry Risk
    Task<IEnumerable<ExpiryRiskDTO>> GetExpiryRiskReportAsync(int daysThreshold = 30, CancellationToken ct = default);
    
    // Reorder Recommendations
    Task<IEnumerable<ReorderRecommendationDTO>> GetReorderRecommendationsAsync(CancellationToken ct = default);
    
    // Backorder Summary
    Task<IEnumerable<BackorderSummaryDTO>> GetBackorderSummaryAsync(CancellationToken ct = default);
    
    // Movement History
    Task<IEnumerable<StockMovementDTO>> GetMovementHistoryAsync(Guid? stockItemId = null, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken ct = default);
    
    // Adjustment History
    Task<IEnumerable<StockAdjustmentDTO>> GetAdjustmentHistoryAsync(Guid? stockItemId = null, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken ct = default);
}
