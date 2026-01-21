using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StockService.Application.DTOs;
using StockService.Application.Repositories;
using StockService.Infrastructure.Persistence;

namespace StockService.Infrastructure.Repositories;

public class ReportRepository : IReportRepository
{
    private readonly StockServiceDbContext _db;
    private readonly IStockServiceDbContextProcedures _sp;
    private readonly ILogger<ReportRepository> _logger;

    public ReportRepository(
        StockServiceDbContext db,
        IStockServiceDbContextProcedures sp,
        ILogger<ReportRepository> logger)
    {
        _db = db;
        _sp = sp;
        _logger = logger;
    }

    public async Task<IEnumerable<StockValuationDTO>> GetStockValuationAsync(
        Guid? warehouseId = null, CancellationToken ct = default)
    {
        var query = _db.VwStockValuations.AsQueryable();
        
        if (warehouseId.HasValue)
            query = query.Where(v => v.WarehouseId == warehouseId);

        var results = await query.ToListAsync(ct);
        
        return results.Select(v => new StockValuationDTO
        {
            ProductId = v.ProductId,
            WarehouseId = v.WarehouseId,
            Quantity = v.AvailableQuantity,
            UnitCost = v.UnitCost,
            TotalValue = v.StockValue
        });
    }

    public async Task<decimal> GetTotalStockValueAsync(Guid? warehouseId = null, CancellationToken ct = default)
    {
        var valuations = await GetStockValuationAsync(warehouseId, ct);
        return valuations.Sum(v => v.TotalValue ?? 0);
    }

    public async Task<IEnumerable<StockAgingSummaryDTO>> GetStockAgingSummaryAsync(CancellationToken ct = default)
    {
        // SP returns raw stock items with DaysInStock - we need to bucket them
        var results = await _sp.SP_GetStockAgingSummaryAsync(cancellationToken: ct);
        
        // Group by age buckets
        var buckets = results
            .GroupBy(r => (r.DaysInStock ?? 0) switch
            {
                <= 30 => "0-30 days",
                <= 60 => "31-60 days",
                <= 90 => "61-90 days",
                _ => "90+ days"
            })
            .Select(g => new StockAgingSummaryDTO
            {
                AgeBucket = g.Key,
                ItemCount = g.Count(),
                TotalQuantity = g.Sum(x => x.AvailableQuantity),
                TotalValue = 0 // Would need to join with cost data
            })
            .ToList();

        return buckets;
    }

    public async Task<IEnumerable<InventoryTurnoverDTO>> GetInventoryTurnoverAsync(CancellationToken ct = default)
    {
        var results = await _sp.SP_GetInventoryTurnoverAsync(cancellationToken: ct);
        
        return results.Select(r => new InventoryTurnoverDTO
        {
            ProductId = Guid.Empty, // SP returns StockItemId
            TurnoverRatio = r.TotalOutbound.HasValue && r.AverageCost > 0 
                ? (decimal)r.TotalOutbound.Value / (r.AverageCost ?? 1) 
                : 0,
            DaysOfInventory = 0,
            TotalSold = r.TotalOutbound ?? 0,
            AverageInventory = 0
        });
    }

    public async Task<IEnumerable<DeadStockDTO>> GetDeadStockAsync(
        int daysSinceLastMovement = 90, CancellationToken ct = default)
    {
        var results = await _sp.SP_IdentifyDeadStockAsync(cancellationToken: ct);
        var now = DateTime.UtcNow;
        
        return results
            .Where(r => r.LastRestockedAt.HasValue && 
                       (now - r.LastRestockedAt.Value).TotalDays >= daysSinceLastMovement)
            .Select(r => new DeadStockDTO
            {
                StockItemId = r.StockItemId,
                ProductId = r.ProductId,
                WarehouseId = r.WarehouseId,
                Quantity = r.AvailableQuantity,
                DaysSinceLastMovement = r.LastRestockedAt.HasValue 
                    ? (int)(now - r.LastRestockedAt.Value).TotalDays 
                    : 0,
                LastMovementDate = r.LastRestockedAt
            });
    }

    public async Task<IEnumerable<LowStockReportDTO>> GetLowStockReportAsync(CancellationToken ct = default)
    {
        var results = await _db.VwLowStockItems.ToListAsync(ct);
        
        return results.Select(r => new LowStockReportDTO
        {
            StockItemId = r.StockItemId,
            ProductId = r.ProductId,
            WarehouseId = r.WarehouseId,
            AvailableQuantity = r.AvailableQuantity,
            MinimumStockLevel = r.MinimumStockLevel ?? 0,
            ShortfallQuantity = (r.MinimumStockLevel ?? 0) - r.AvailableQuantity
        });
    }

    public async Task<IEnumerable<ExpiryRiskDTO>> GetExpiryRiskReportAsync(
        int daysThreshold = 30, CancellationToken ct = default)
    {
        // Get from StockItems directly since view might not have all fields
        var now = DateOnly.FromDateTime(DateTime.UtcNow);
        var thresholdDate = now.AddDays(daysThreshold);
        
        var results = await _db.StockItems
            .Include(s => s.Warehouse)
            .Where(s => s.ExpiryDate.HasValue && s.ExpiryDate.Value <= thresholdDate && s.IsActive)
            .ToListAsync(ct);

        return results.Select(r => new ExpiryRiskDTO
        {
            StockItemId = r.StockItemId,
            ProductId = r.ProductId,
            Sku = r.Sku,
            BatchNumber = r.BatchNumber,
            WarehouseId = r.WarehouseId,
            WarehouseName = r.Warehouse?.WarehouseName,
            Quantity = r.AvailableQuantity,
            ExpiryDate = r.ExpiryDate ?? DateOnly.MinValue,
            DaysUntilExpiry = r.ExpiryDate.HasValue ? r.ExpiryDate.Value.DayNumber - now.DayNumber : 0,
            RiskLevel = r.ExpiryDate.HasValue 
                ? (r.ExpiryDate.Value.DayNumber - now.DayNumber) switch
                {
                    <= 7 => "Critical",
                    <= 14 => "High",
                    <= 30 => "Medium",
                    _ => "Low"
                }
                : "Unknown"
        });
    }

    public async Task<IEnumerable<ReorderRecommendationDTO>> GetReorderRecommendationsAsync(CancellationToken ct = default)
    {
        var results = await _db.VwReorderRecommendations.ToListAsync(ct);
        
        return results.Select(r => new ReorderRecommendationDTO
        {
            ProductId = r.ProductId
        });
    }

    public async Task<IEnumerable<BackorderSummaryDTO>> GetBackorderSummaryAsync(CancellationToken ct = default)
    {
        var results = await _db.VwBackorderSummaries.ToListAsync(ct);
        
        return results.Select(r => new BackorderSummaryDTO
        {
            ProductId = r.ProductId
        });
    }

    public async Task<IEnumerable<StockMovementDTO>> GetMovementHistoryAsync(
        Guid? stockItemId = null, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken ct = default)
    {
        var query = _db.VwStockMovementHistories.AsQueryable();

        if (stockItemId.HasValue)
            query = query.Where(m => m.StockItemId == stockItemId);
        
        if (fromDate.HasValue)
            query = query.Where(m => m.MovementDate >= fromDate.Value);
        
        if (toDate.HasValue)
            query = query.Where(m => m.MovementDate <= toDate.Value);

        var results = await query
            .OrderByDescending(m => m.MovementDate)
            .Take(1000)
            .ToListAsync(ct);

        return results.Select(m => new StockMovementDTO
        {
            MovementId = m.MovementId,
            StockItemId = m.StockItemId,
            ProductId = m.ProductId,
            MovementType = m.MovementType,
            Quantity = m.Quantity,
            ReferenceType = m.ReferenceType,
            ReferenceId = m.ReferenceId,
            Reason = m.Reason,
            MovementDate = m.MovementDate
        });
    }

    public async Task<IEnumerable<StockAdjustmentDTO>> GetAdjustmentHistoryAsync(
        Guid? stockItemId = null, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken ct = default)
    {
        var query = _db.VwStockAdjustmentHistories.AsQueryable();

        if (stockItemId.HasValue)
            query = query.Where(a => a.StockItemId == stockItemId);
        
        if (fromDate.HasValue)
            query = query.Where(a => a.AdjustmentDate >= fromDate.Value);
        
        if (toDate.HasValue)
            query = query.Where(a => a.AdjustmentDate <= toDate.Value);

        var results = await query
            .OrderByDescending(a => a.AdjustmentDate)
            .Take(1000)
            .ToListAsync(ct);

        return results.Select(a => new StockAdjustmentDTO
        {
            AdjustmentId = a.AdjustmentId,
            StockItemId = a.StockItemId,
            ProductId = a.ProductId,
            AdjustmentType = a.AdjustmentType,
            AdjustmentQuantity = a.AdjustmentQuantity,
            QuantityBefore = a.QuantityBefore,
            QuantityAfter = a.QuantityAfter,
            Reason = a.Reason,
            PerformedBy = a.PerformedBy,
            ApprovedBy = a.ApprovedBy,
            AdjustmentDate = a.AdjustmentDate
        });
    }
}
