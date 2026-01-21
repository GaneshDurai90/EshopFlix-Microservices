using StockService.Application.CQRS;
using StockService.Application.DTOs;

namespace StockService.Application.Commands;

// ============ Reservation Commands ============

/// <summary>
/// Reserve stock for a cart or order.
/// </summary>
public record ReserveStockCommand(
    Guid ProductId,
    Guid? VariationId,
    Guid? WarehouseId,
    Guid? CartId,
    Guid? OrderId,
    Guid? CustomerId,
    int Quantity,
    string ReservationType = "Cart",
    int? TtlMinutes = null,
    string? IdempotencyKey = null
) : ICommand<CreateReservationResponse>;

/// <summary>
/// Commit a pending reservation when order is placed.
/// </summary>
public record CommitReservationCommand(
    Guid ReservationId,
    Guid OrderId,
    string? IdempotencyKey = null
) : ICommand<bool>;

/// <summary>
/// Release a specific reservation.
/// </summary>
public record ReleaseReservationCommand(
    Guid ReservationId,
    string? Reason = null,
    string? IdempotencyKey = null
) : ICommand<bool>;

/// <summary>
/// Release all reservations for a cart.
/// </summary>
public record ReleaseCartReservationsCommand(
    Guid CartId,
    string? IdempotencyKey = null
) : ICommand<int>;

// ============ Stock Adjustment Commands ============

/// <summary>
/// Increase stock quantity.
/// </summary>
public record IncreaseStockCommand(
    Guid StockItemId,
    int Quantity,
    string Reason,
    Guid PerformedBy,
    string? IdempotencyKey = null
) : ICommand<StockAdjustmentDTO>;

/// <summary>
/// Decrease stock quantity.
/// </summary>
public record DecreaseStockCommand(
    Guid StockItemId,
    int Quantity,
    string Reason,
    Guid PerformedBy,
    string? IdempotencyKey = null
) : ICommand<StockAdjustmentDTO>;

/// <summary>
/// Manual stock adjustment with approval.
/// </summary>
public record AdjustStockCommand(
    Guid StockItemId,
    string AdjustmentType,
    int AdjustmentQuantity,
    string? Reason,
    string? Notes,
    Guid PerformedBy,
    Guid? ApprovedBy,
    string? IdempotencyKey = null
) : ICommand<StockAdjustmentDTO>;

// ============ Stock Item Commands ============

/// <summary>
/// Create a new stock item in a warehouse.
/// </summary>
public record CreateStockItemCommand(
    Guid ProductId,
    Guid? VariationId,
    Guid WarehouseId,
    string? Sku,
    int InitialQuantity,
    int? MinimumStockLevel,
    int? MaximumStockLevel,
    int? ReorderQuantity,
    decimal? UnitCost,
    DateOnly? ExpiryDate,
    string? BatchNumber,
    string? BinLocation,
    string? IdempotencyKey = null
) : ICommand<StockItemDTO>;

/// <summary>
/// Update stock item settings.
/// </summary>
public record UpdateStockItemCommand(
    Guid StockItemId,
    int? MinimumStockLevel,
    int? MaximumStockLevel,
    int? ReorderQuantity,
    decimal? UnitCost,
    string? BinLocation,
    bool? IsActive,
    string? IdempotencyKey = null
) : ICommand<StockItemDTO>;

// ============ Warehouse Commands ============

/// <summary>
/// Create a new warehouse.
/// </summary>
public record CreateWarehouseCommand(
    string WarehouseName,
    string WarehouseCode,
    string? Address,
    string Type,
    int Priority,
    int? Capacity,
    string? ContactDetails,
    string? OperatingHours,
    string? IdempotencyKey = null
) : ICommand<WarehouseDTO>;

/// <summary>
/// Update warehouse settings.
/// </summary>
public record UpdateWarehouseCommand(
    Guid WarehouseId,
    string? WarehouseName,
    string? Address,
    bool? IsActive,
    int? Priority,
    int? Capacity,
    string? ContactDetails,
    string? OperatingHours,
    string? IdempotencyKey = null
) : ICommand<WarehouseDTO>;

// ============ Alert Commands ============

/// <summary>
/// Acknowledge a stock alert.
/// </summary>
public record AcknowledgeAlertCommand(
    Guid AlertId,
    Guid AcknowledgedBy,
    string? IdempotencyKey = null
) : ICommand<bool>;

/// <summary>
/// Trigger alert checks (low stock, overstock, expiry).
/// </summary>
public record TriggerAlertsCommand(
    string? IdempotencyKey = null
) : ICommand<int>;

// ============ Background Job Commands ============

/// <summary>
/// Release expired reservations.
/// </summary>
public record ReleaseExpiredReservationsCommand : ICommand<int>;

/// <summary>
/// Expire stock batches past their expiry date.
/// </summary>
public record ExpireStockBatchesCommand : ICommand<int>;

/// <summary>
/// Recalculate safety stock levels based on demand.
/// </summary>
public record RecalculateSafetyStockCommand : ICommand<int>;
