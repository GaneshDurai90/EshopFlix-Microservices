using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StockService.Application.Repositories;
using StockService.Domain.Entities;
using StockService.Infrastructure.Persistence;

namespace StockService.Infrastructure.Repositories;

/// <summary>
/// Repository for purchase order operations.
/// </summary>
public class PurchaseOrderRepository : IPurchaseOrderRepository
{
    private readonly StockServiceDbContext _db;
    private readonly ILogger<PurchaseOrderRepository> _logger;

    public PurchaseOrderRepository(StockServiceDbContext db, ILogger<PurchaseOrderRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<PurchaseOrder?> GetByIdAsync(Guid purchaseOrderId, CancellationToken ct = default)
    {
        return await _db.PurchaseOrders
            .Include(p => p.Supplier)
            .Include(p => p.Warehouse)
            .FirstOrDefaultAsync(p => p.PurchaseOrderId == purchaseOrderId, ct);
    }

    public async Task<PurchaseOrder?> GetByIdWithItemsAsync(Guid purchaseOrderId, CancellationToken ct = default)
    {
        return await _db.PurchaseOrders
            .Include(p => p.Supplier)
            .Include(p => p.Warehouse)
            .Include(p => p.PurchaseOrderItems)
            .FirstOrDefaultAsync(p => p.PurchaseOrderId == purchaseOrderId, ct);
    }

    public async Task<PurchaseOrder?> GetByPoNumberAsync(string poNumber, CancellationToken ct = default)
    {
        return await _db.PurchaseOrders
            .Include(p => p.Supplier)
            .Include(p => p.Warehouse)
            .FirstOrDefaultAsync(p => p.Ponumber == poNumber, ct);
    }

    public async Task<IEnumerable<PurchaseOrder>> GetBySupplierAsync(Guid supplierId, CancellationToken ct = default)
    {
        return await _db.PurchaseOrders
            .Include(p => p.Supplier)
            .Include(p => p.Warehouse)
            .Where(p => p.SupplierId == supplierId)
            .OrderByDescending(p => p.OrderDate)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<PurchaseOrder>> GetByWarehouseAsync(Guid warehouseId, CancellationToken ct = default)
    {
        return await _db.PurchaseOrders
            .Include(p => p.Supplier)
            .Include(p => p.Warehouse)
            .Where(p => p.WarehouseId == warehouseId)
            .OrderByDescending(p => p.OrderDate)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<PurchaseOrder>> GetByStatusAsync(string status, CancellationToken ct = default)
    {
        return await _db.PurchaseOrders
            .Include(p => p.Supplier)
            .Include(p => p.Warehouse)
            .Where(p => p.OrderStatus == status)
            .OrderByDescending(p => p.OrderDate)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<PurchaseOrder>> GetPendingDeliveriesAsync(CancellationToken ct = default)
    {
        return await _db.PurchaseOrders
            .Include(p => p.Supplier)
            .Include(p => p.Warehouse)
            .Where(p => p.OrderStatus == "Ordered" || p.OrderStatus == "Shipped")
            .OrderBy(p => p.ExpectedDeliveryDate)
            .ToListAsync(ct);
    }

    public async Task<PurchaseOrder> CreateAsync(PurchaseOrder purchaseOrder, CancellationToken ct = default)
    {
        _db.PurchaseOrders.Add(purchaseOrder);
        await _db.SaveChangesAsync(ct);
        
        _logger.LogInformation("Created purchase order {PoNumber} for supplier {SupplierId}", 
            purchaseOrder.Ponumber, purchaseOrder.SupplierId);
        
        return purchaseOrder;
    }

    public async Task AddItemAsync(PurchaseOrderItem item, CancellationToken ct = default)
    {
        _db.PurchaseOrderItems.Add(item);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(PurchaseOrder purchaseOrder, CancellationToken ct = default)
    {
        purchaseOrder.UpdatedAt = DateTime.UtcNow;
        _db.Entry(purchaseOrder).State = EntityState.Modified;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<int> ReceiveItemAsync(Guid poItemId, int receivedQuantity, Guid performedBy, CancellationToken ct = default)
    {
        var item = await _db.PurchaseOrderItems.FindAsync(new object[] { poItemId }, ct);
        if (item == null) return 0;

        item.ReceivedQuantity = (item.ReceivedQuantity ?? 0) + receivedQuantity;
        await _db.SaveChangesAsync(ct);
        
        _logger.LogInformation("Received {Quantity} units for PO item {PoItemId}", receivedQuantity, poItemId);
        return receivedQuantity;
    }

    public async Task<string> GeneratePoNumberAsync(CancellationToken ct = default)
    {
        var today = DateTime.UtcNow;
        var prefix = $"PO-{today:yyyyMMdd}-";
        
        var lastPo = await _db.PurchaseOrders
            .Where(p => p.Ponumber.StartsWith(prefix))
            .OrderByDescending(p => p.Ponumber)
            .FirstOrDefaultAsync(ct);

        int sequence = 1;
        if (lastPo != null)
        {
            var lastSeq = lastPo.Ponumber.Split('-').LastOrDefault();
            if (int.TryParse(lastSeq, out var parsed))
            {
                sequence = parsed + 1;
            }
        }

        return $"{prefix}{sequence:D4}";
    }
}
