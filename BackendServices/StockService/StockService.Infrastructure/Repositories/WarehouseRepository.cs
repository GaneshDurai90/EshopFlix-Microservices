using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StockService.Application.Repositories;
using StockService.Domain.Entities;
using StockService.Infrastructure.Persistence;

namespace StockService.Infrastructure.Repositories;

public class WarehouseRepository : IWarehouseRepository
{
    private readonly StockServiceDbContext _db;
    private readonly ILogger<WarehouseRepository> _logger;

    public WarehouseRepository(StockServiceDbContext db, ILogger<WarehouseRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Warehouse?> GetByIdAsync(Guid warehouseId, CancellationToken ct = default)
    {
        return await _db.Warehouses.FindAsync(new object[] { warehouseId }, ct);
    }

    public async Task<Warehouse?> GetByCodeAsync(string warehouseCode, CancellationToken ct = default)
    {
        return await _db.Warehouses
            .FirstOrDefaultAsync(w => w.WarehouseCode == warehouseCode, ct);
    }

    public async Task<IEnumerable<Warehouse>> GetAllActiveAsync(CancellationToken ct = default)
    {
        return await _db.Warehouses
            .Where(w => w.IsActive)
            .OrderBy(w => w.Priority)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<Warehouse>> GetByTypeAsync(string type, CancellationToken ct = default)
    {
        return await _db.Warehouses
            .Where(w => w.Type == type && w.IsActive)
            .OrderBy(w => w.Priority)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<Warehouse>> GetByPriorityOrderAsync(CancellationToken ct = default)
    {
        return await _db.Warehouses
            .Where(w => w.IsActive)
            .OrderBy(w => w.Priority)
            .ToListAsync(ct);
    }

    public async Task<Warehouse> CreateAsync(Warehouse warehouse, CancellationToken ct = default)
    {
        _db.Warehouses.Add(warehouse);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Created warehouse {WarehouseId}: {WarehouseCode}", 
            warehouse.WarehouseId, warehouse.WarehouseCode);
        return warehouse;
    }

    public async Task UpdateAsync(Warehouse warehouse, CancellationToken ct = default)
    {
        _db.Entry(warehouse).State = EntityState.Modified;
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Updated warehouse {WarehouseId}", warehouse.WarehouseId);
    }

    public async Task<bool> SetActiveStatusAsync(Guid warehouseId, bool isActive, CancellationToken ct = default)
    {
        var warehouse = await GetByIdAsync(warehouseId, ct);
        if (warehouse is null) return false;

        warehouse.IsActive = isActive;
        warehouse.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        
        _logger.LogInformation("Set warehouse {WarehouseId} active status to {IsActive}", warehouseId, isActive);
        return true;
    }
}
