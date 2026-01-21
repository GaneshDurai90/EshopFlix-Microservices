using StockService.Application.DTOs;

namespace StockService.Application.Services.Abstractions;

public interface IStockAppService
{
    // ============ Stock Item Operations ============
    Task<StockItemDTO?> GetStockItemAsync(Guid stockItemId, CancellationToken ct = default);
    Task<IEnumerable<StockItemDTO>> GetStockByProductAsync(Guid productId, Guid? variationId = null, CancellationToken ct = default);
    Task<IEnumerable<StockItemDTO>> GetStockByWarehouseAsync(Guid warehouseId, CancellationToken ct = default);
    Task<StockItemSummaryDTO?> GetStockSummaryByProductAsync(Guid productId, Guid? variationId = null, CancellationToken ct = default);
    Task<StockItemDTO> CreateStockItemAsync(CreateStockItemRequest request, CancellationToken ct = default);
    Task<StockItemDTO> UpdateStockItemAsync(UpdateStockItemRequest request, CancellationToken ct = default);

    // ============ Availability & Allocation ============
    Task<StockAvailabilityDTO?> GetAvailabilityAsync(Guid productId, Guid? variationId = null, CancellationToken ct = default);
    Task<CheckAvailabilityResponse> CheckAvailabilityAsync(CheckAvailabilityRequest request, CancellationToken ct = default);
    Task<IEnumerable<AllocationSuggestionDTO>> GetAllocationSuggestionsAsync(Guid productId, Guid? variationId, int quantity, CancellationToken ct = default);

    // ============ Reservation Operations ============
    Task<CreateReservationResponse> ReserveStockAsync(CreateReservationRequest request, CancellationToken ct = default);
    Task<bool> CommitReservationAsync(CommitReservationRequest request, CancellationToken ct = default);
    Task<bool> ReleaseReservationAsync(ReleaseReservationRequest request, CancellationToken ct = default);
    Task<bool> ReleaseReservationsByCartAsync(Guid cartId, CancellationToken ct = default);
    Task<IEnumerable<StockReservationDTO>> GetReservationsByCartAsync(Guid cartId, CancellationToken ct = default);
    Task<IEnumerable<StockReservationDTO>> GetReservationsByOrderAsync(Guid orderId, CancellationToken ct = default);

    // ============ Stock Adjustments ============
    Task<StockAdjustmentDTO> IncreaseStockAsync(Guid stockItemId, int quantity, string reason, Guid performedBy, CancellationToken ct = default);
    Task<StockAdjustmentDTO> DecreaseStockAsync(Guid stockItemId, int quantity, string reason, Guid performedBy, CancellationToken ct = default);
    Task<StockAdjustmentDTO> AdjustStockAsync(CreateAdjustmentRequest request, CancellationToken ct = default);

    // ============ Warehouse Operations ============
    Task<WarehouseDTO?> GetWarehouseAsync(Guid warehouseId, CancellationToken ct = default);
    Task<IEnumerable<WarehouseDTO>> GetAllWarehousesAsync(CancellationToken ct = default);
    Task<WarehouseDTO> CreateWarehouseAsync(CreateWarehouseRequest request, CancellationToken ct = default);
    Task<WarehouseDTO> UpdateWarehouseAsync(UpdateWarehouseRequest request, CancellationToken ct = default);

    // ============ Transfer Operations ============
    Task<StockTransferDTO> CreateTransferAsync(CreateTransferRequest request, CancellationToken ct = default);
    Task<StockTransferDTO?> GetTransferAsync(Guid transferId, CancellationToken ct = default);
    Task<IEnumerable<StockTransferDTO>> GetTransfersByWarehouseAsync(Guid warehouseId, bool isSource, CancellationToken ct = default);
    Task<bool> ShipTransferAsync(ShipTransferRequest request, CancellationToken ct = default);
    Task<bool> ReceiveTransferAsync(ReceiveTransferRequest request, CancellationToken ct = default);

    // ============ Alerts ============
    Task<IEnumerable<StockAlertDTO>> GetActiveAlertsAsync(CancellationToken ct = default);
    Task<IEnumerable<StockAlertDTO>> GetAlertsByTypeAsync(string alertType, CancellationToken ct = default);
    Task<bool> AcknowledgeAlertAsync(AcknowledgeAlertRequest request, CancellationToken ct = default);
    Task<int> TriggerAlertsAsync(CancellationToken ct = default);

    // ============ Purchase Orders ============
    Task<PurchaseOrderDTO> CreatePurchaseOrderAsync(CreatePurchaseOrderRequest request, CancellationToken ct = default);
    Task<PurchaseOrderDTO?> GetPurchaseOrderAsync(Guid purchaseOrderId, CancellationToken ct = default);
    Task<IEnumerable<PurchaseOrderDTO>> GetPurchaseOrdersByStatusAsync(string status, CancellationToken ct = default);
    Task<bool> ReceivePurchaseOrderAsync(ReceivePurchaseOrderRequest request, CancellationToken ct = default);

    // ============ Suppliers ============
    Task<SupplierDTO?> GetSupplierAsync(Guid supplierId, CancellationToken ct = default);
    Task<IEnumerable<SupplierDTO>> GetAllSuppliersAsync(CancellationToken ct = default);
    Task<SupplierDTO> CreateSupplierAsync(CreateSupplierRequest request, CancellationToken ct = default);

    // ============ Reports ============
    Task<IEnumerable<StockValuationDTO>> GetStockValuationReportAsync(Guid? warehouseId = null, CancellationToken ct = default);
    Task<IEnumerable<StockAgingSummaryDTO>> GetStockAgingReportAsync(CancellationToken ct = default);
    Task<IEnumerable<InventoryTurnoverDTO>> GetInventoryTurnoverReportAsync(CancellationToken ct = default);
    Task<IEnumerable<DeadStockDTO>> GetDeadStockReportAsync(int daysSinceLastMovement = 90, CancellationToken ct = default);
    Task<IEnumerable<LowStockReportDTO>> GetLowStockReportAsync(CancellationToken ct = default);
    Task<IEnumerable<ExpiryRiskDTO>> GetExpiryRiskReportAsync(int daysThreshold = 30, CancellationToken ct = default);
    Task<IEnumerable<ReorderRecommendationDTO>> GetReorderRecommendationsAsync(CancellationToken ct = default);
    Task<IEnumerable<BackorderSummaryDTO>> GetBackorderSummaryAsync(CancellationToken ct = default);
    Task<IEnumerable<StockMovementDTO>> GetMovementHistoryAsync(Guid? stockItemId = null, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken ct = default);

    // ============ Background Jobs ============
    Task<int> ReleaseExpiredReservationsAsync(CancellationToken ct = default);
    Task<int> ExpireStockBatchesAsync(CancellationToken ct = default);
    Task<int> RecalculateSafetyStockAsync(CancellationToken ct = default);
}

// Request DTOs not defined elsewhere
public record CreateStockItemRequest
{
    public Guid ProductId { get; init; }
    public Guid? VariationId { get; init; }
    public Guid WarehouseId { get; init; }
    public string? Sku { get; init; }
    public int InitialQuantity { get; init; }
    public int? MinimumStockLevel { get; init; }
    public int? MaximumStockLevel { get; init; }
    public int? ReorderQuantity { get; init; }
    public decimal? UnitCost { get; init; }
    public DateOnly? ExpiryDate { get; init; }
    public string? BatchNumber { get; init; }
    public string? BinLocation { get; init; }
}

public record UpdateStockItemRequest
{
    public Guid StockItemId { get; init; }
    public int? MinimumStockLevel { get; init; }
    public int? MaximumStockLevel { get; init; }
    public int? ReorderQuantity { get; init; }
    public decimal? UnitCost { get; init; }
    public string? BinLocation { get; init; }
    public bool? IsActive { get; init; }
}
