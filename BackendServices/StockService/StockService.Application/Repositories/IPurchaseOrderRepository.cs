using StockService.Application.DTOs;
using StockService.Domain.Entities;

namespace StockService.Application.Repositories;

public interface IPurchaseOrderRepository
{
    Task<PurchaseOrder?> GetByIdAsync(Guid purchaseOrderId, CancellationToken ct = default);
    Task<PurchaseOrder?> GetByIdWithItemsAsync(Guid purchaseOrderId, CancellationToken ct = default);
    Task<PurchaseOrder?> GetByPoNumberAsync(string poNumber, CancellationToken ct = default);
    Task<IEnumerable<PurchaseOrder>> GetBySupplierAsync(Guid supplierId, CancellationToken ct = default);
    Task<IEnumerable<PurchaseOrder>> GetByWarehouseAsync(Guid warehouseId, CancellationToken ct = default);
    Task<IEnumerable<PurchaseOrder>> GetByStatusAsync(string status, CancellationToken ct = default);
    Task<IEnumerable<PurchaseOrder>> GetPendingDeliveriesAsync(CancellationToken ct = default);
    Task<PurchaseOrder> CreateAsync(PurchaseOrder purchaseOrder, CancellationToken ct = default);
    Task AddItemAsync(PurchaseOrderItem item, CancellationToken ct = default);
    Task UpdateAsync(PurchaseOrder purchaseOrder, CancellationToken ct = default);
    Task<int> ReceiveItemAsync(Guid poItemId, int receivedQuantity, Guid performedBy, CancellationToken ct = default);
    Task<string> GeneratePoNumberAsync(CancellationToken ct = default);
}
