using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StockService.Application.DTOs;
using StockService.Application.Repositories;
using StockService.Domain.Entities;
using StockService.Infrastructure.Persistence;

namespace StockService.Infrastructure.Repositories;

public class StockRepository : IStockRepository
{
    private readonly StockServiceDbContext _db;
    private readonly IStockServiceDbContextProcedures _sp;
    private readonly ILogger<StockRepository> _logger;

    public StockRepository(
        StockServiceDbContext db,
        IStockServiceDbContextProcedures sp,
        ILogger<StockRepository> logger)
    {
        _db = db;
        _sp = sp;
        _logger = logger;
    }

    public async Task<StockItem?> GetByIdAsync(Guid stockItemId, CancellationToken ct = default)
    {
        return await _db.StockItems
            .Include(s => s.Warehouse)
            .FirstOrDefaultAsync(s => s.StockItemId == stockItemId, ct);
    }

    public async Task<StockItem?> GetByProductAndWarehouseAsync(
        Guid productId, Guid? variationId, Guid warehouseId, CancellationToken ct = default)
    {
        return await _db.StockItems
            .Include(s => s.Warehouse)
            .FirstOrDefaultAsync(s => 
                s.ProductId == productId && 
                s.VariationId == variationId && 
                s.WarehouseId == warehouseId && 
                s.IsActive, ct);
    }

    public async Task<IEnumerable<StockItem>> GetByProductAsync(
        Guid productId, Guid? variationId = null, CancellationToken ct = default)
    {
        var query = _db.StockItems
            .Include(s => s.Warehouse)
            .Where(s => s.ProductId == productId && s.IsActive);

        if (variationId.HasValue)
            query = query.Where(s => s.VariationId == variationId);

        return await query.ToListAsync(ct);
    }

    public async Task<IEnumerable<StockItem>> GetByWarehouseAsync(Guid warehouseId, CancellationToken ct = default)
    {
        return await _db.StockItems
            .Include(s => s.Warehouse)
            .Where(s => s.WarehouseId == warehouseId && s.IsActive)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<StockItem>> GetLowStockItemsAsync(CancellationToken ct = default)
    {
        return await _db.StockItems
            .Include(s => s.Warehouse)
            .Where(s => s.IsActive && 
                        s.MinimumStockLevel.HasValue && 
                        s.AvailableQuantity <= s.MinimumStockLevel.Value)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<StockItem>> GetExpiringSoonAsync(int daysThreshold = 30, CancellationToken ct = default)
    {
        var thresholdDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(daysThreshold));
        return await _db.StockItems
            .Include(s => s.Warehouse)
            .Where(s => s.IsActive && 
                        s.ExpiryDate.HasValue && 
                        s.ExpiryDate.Value <= thresholdDate)
            .ToListAsync(ct);
    }

    public async Task<StockItem> CreateAsync(StockItem stockItem, CancellationToken ct = default)
    {
        _db.StockItems.Add(stockItem);
        await _db.SaveChangesAsync(ct);
        return stockItem;
    }

    public async Task UpdateAsync(StockItem stockItem, CancellationToken ct = default)
    {
        _db.Entry(stockItem).State = EntityState.Modified;
        await _db.SaveChangesAsync(ct);
    }

    // Stock Operations via Stored Procedures
    public async Task<int> IncreaseStockAsync(
        Guid stockItemId, int quantity, string reason, Guid performedBy, CancellationToken ct = default)
    {
        _logger.LogInformation("SP_IncreaseStock: {StockItemId}, {Quantity}", stockItemId, quantity);
        return await _sp.SP_IncreaseStockAsync(stockItemId, quantity, reason, performedBy, cancellationToken: ct);
    }

    public async Task<int> DecreaseStockAsync(
        Guid stockItemId, int quantity, string reason, Guid performedBy, CancellationToken ct = default)
    {
        _logger.LogInformation("SP_DecreaseStock: {StockItemId}, {Quantity}", stockItemId, quantity);
        return await _sp.SP_DecreaseStockAsync(stockItemId, quantity, reason, performedBy, cancellationToken: ct);
    }

    public async Task<int> AdjustStockAsync(
        Guid stockItemId, int adjustmentQuantity, string reason, Guid performedBy, Guid? approvedBy, CancellationToken ct = default)
    {
        _logger.LogInformation("SP_AdjustStock: {StockItemId}, {Quantity}", stockItemId, adjustmentQuantity);
        return await _sp.SP_AdjustStockAsync(stockItemId, adjustmentQuantity, reason, performedBy, approvedBy, cancellationToken: ct);
    }

    // Reservation Operations - Update stock quantities only (reservation record already created)
    public async Task<int> ReserveStockAsync(Guid stockItemId, Guid reservationId, int quantity, CancellationToken ct = default)
    {
        _logger.LogInformation("ReserveStock: {StockItemId}, {ReservationId}, {Quantity}", 
            stockItemId, reservationId, quantity);
        
        // Update stock quantities directly instead of using SP (SP also inserts reservation which causes duplicate)
        var stockItem = await _db.StockItems.FindAsync(new object[] { stockItemId }, ct);
        if (stockItem == null)
        {
            _logger.LogWarning("Stock item {StockItemId} not found", stockItemId);
            return 0;
        }

        if (stockItem.AvailableQuantity < quantity)
        {
            _logger.LogWarning("Insufficient stock for {StockItemId}. Available: {Available}, Requested: {Requested}",
                stockItemId, stockItem.AvailableQuantity, quantity);
            return 0;
        }

        stockItem.AvailableQuantity -= quantity;
        stockItem.ReservedQuantity += quantity;
        stockItem.UpdatedAt = DateTime.UtcNow;

        return await _db.SaveChangesAsync(ct);
    }

    public async Task<int> CommitReservationAsync(Guid reservationId, CancellationToken ct = default)
    {
        _logger.LogInformation("SP_CommitStockReservation: {ReservationId}", reservationId);
        return await _sp.SP_CommitStockReservationAsync(reservationId, cancellationToken: ct);
    }

    public async Task<int> ReleaseExpiredReservationsAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("SP_ReleaseExpiredReservations");
        return await _sp.SP_ReleaseExpiredReservationsAsync(cancellationToken: ct);
    }

    // Alerts
    public async Task<int> TriggerLowStockAlertsAsync(CancellationToken ct = default)
    {
        return await _sp.SP_TriggerLowStockAlertsAsync(cancellationToken: ct);
    }

    public async Task<int> TriggerOverStockAlertsAsync(CancellationToken ct = default)
    {
        return await _sp.SP_TriggerOverStockAlertsAsync(cancellationToken: ct);
    }

    // Batch Operations
    public async Task<int> ExpireStockBatchesAsync(CancellationToken ct = default)
    {
        return await _sp.SP_ExpireStockBatchesAsync(cancellationToken: ct);
    }

    public async Task<int> RecalculateSafetyStockAsync(CancellationToken ct = default)
    {
        return await _sp.SP_RecalculateSafetyStockAsync(cancellationToken: ct);
    }

    // Availability Check
    public async Task<CheckAvailabilityResponse> CheckAvailabilityAsync(
        CheckAvailabilityRequest request, CancellationToken ct = default)
    {
        var query = _db.StockItems
            .Include(s => s.Warehouse)
            .Where(s => s.ProductId == request.ProductId && s.IsActive && s.AvailableQuantity > 0);

        if (request.VariationId.HasValue)
            query = query.Where(s => s.VariationId == request.VariationId);

        // Order by preferred warehouse first, then by priority
        if (request.PreferredWarehouseId.HasValue)
        {
            query = query.OrderByDescending(s => s.WarehouseId == request.PreferredWarehouseId)
                         .ThenBy(s => s.Warehouse.Priority);
        }
        else
        {
            query = query.OrderBy(s => s.Warehouse.Priority);
        }

        var stockItems = await query.ToListAsync(ct);
        var totalAvailable = stockItems.Sum(s => s.AvailableQuantity);
        var allocations = new List<AllocationSuggestionDTO>();
        var remaining = request.Quantity;

        foreach (var item in stockItems)
        {
            if (remaining <= 0) break;

            var allocate = Math.Min(remaining, item.AvailableQuantity);
            allocations.Add(new AllocationSuggestionDTO
            {
                WarehouseId = item.WarehouseId,
                WarehouseName = item.Warehouse?.WarehouseName ?? "Unknown",
                StockItemId = item.StockItemId,
                AllocatedQuantity = allocate,
                Priority = item.Warehouse?.Priority ?? 999
            });
            remaining -= allocate;
        }

        return new CheckAvailabilityResponse
        {
            IsAvailable = remaining <= 0,
            AvailableQuantity = totalAvailable,
            RequestedQuantity = request.Quantity,
            Allocations = allocations,
            Message = remaining <= 0 
                ? "Stock available" 
                : $"Insufficient stock. Short by {remaining} units."
        };
    }

    public async Task<StockAvailabilityDTO?> GetAvailabilityAsync(
        Guid productId, Guid? variationId, CancellationToken ct = default)
    {
        var query = _db.StockItems
            .Include(s => s.Warehouse)
            .Where(s => s.ProductId == productId && s.IsActive);

        if (variationId.HasValue)
            query = query.Where(s => s.VariationId == variationId);

        var items = await query.ToListAsync(ct);
        if (!items.Any()) return null;

        var totalAvailable = items.Sum(s => s.AvailableQuantity);
        var totalReserved = items.Sum(s => s.ReservedQuantity);

        return new StockAvailabilityDTO
        {
            ProductId = productId,
            VariationId = variationId,
            Sku = items.FirstOrDefault()?.Sku,
            TotalAvailable = totalAvailable,
            TotalReserved = totalReserved,
            IsInStock = totalAvailable > 0,
            IsLowStock = items.Any(i => i.MinimumStockLevel.HasValue && i.AvailableQuantity <= i.MinimumStockLevel.Value),
            WarehouseBreakdown = items.Select(i => new WarehouseStockDTO
            {
                WarehouseId = i.WarehouseId,
                WarehouseName = i.Warehouse?.WarehouseName ?? "Unknown",
                WarehouseCode = i.Warehouse?.WarehouseCode ?? "N/A",
                AvailableQuantity = i.AvailableQuantity,
                ReservedQuantity = i.ReservedQuantity,
                Priority = i.Warehouse?.Priority ?? 999
            }).ToList()
        };
    }
}
