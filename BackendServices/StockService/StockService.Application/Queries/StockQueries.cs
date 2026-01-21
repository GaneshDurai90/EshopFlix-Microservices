using StockService.Application.CQRS;
using StockService.Application.DTOs;

namespace StockService.Application.Queries;

// ============ Stock Item Queries ============

/// <summary>
/// Get a stock item by ID.
/// </summary>
public record GetStockItemQuery(Guid StockItemId) : IQuery<StockItemDTO?>;

/// <summary>
/// Get all stock for a product across warehouses.
/// </summary>
public record GetStockByProductQuery(
    Guid ProductId,
    Guid? VariationId = null
) : IQuery<IEnumerable<StockItemDTO>>;

/// <summary>
/// Get stock summary for a product.
/// </summary>
public record GetStockSummaryQuery(
    Guid ProductId,
    Guid? VariationId = null
) : IQuery<StockItemSummaryDTO?>;

/// <summary>
/// Get all stock in a warehouse.
/// </summary>
public record GetStockByWarehouseQuery(Guid WarehouseId) : IQuery<IEnumerable<StockItemDTO>>;

// ============ Availability Queries ============

/// <summary>
/// Get stock availability for a product.
/// </summary>
public record GetAvailabilityQuery(
    Guid ProductId,
    Guid? VariationId = null
) : IQuery<StockAvailabilityDTO?>;

/// <summary>
/// Check availability and get allocation suggestions.
/// </summary>
public record CheckAvailabilityQuery(
    Guid ProductId,
    Guid? VariationId,
    int Quantity,
    Guid? PreferredWarehouseId = null
) : IQuery<CheckAvailabilityResponse>;

// ============ Reservation Queries ============

/// <summary>
/// Get reservations for a cart.
/// </summary>
public record GetCartReservationsQuery(Guid CartId) : IQuery<IEnumerable<StockReservationDTO>>;

/// <summary>
/// Get reservations for an order.
/// </summary>
public record GetOrderReservationsQuery(Guid OrderId) : IQuery<IEnumerable<StockReservationDTO>>;

// ============ Warehouse Queries ============

/// <summary>
/// Get warehouse by ID.
/// </summary>
public record GetWarehouseQuery(Guid WarehouseId) : IQuery<WarehouseDTO?>;

/// <summary>
/// Get all active warehouses.
/// </summary>
public record GetWarehousesQuery : IQuery<IEnumerable<WarehouseDTO>>;

// ============ Alert Queries ============

/// <summary>
/// Get all active alerts.
/// </summary>
public record GetActiveAlertsQuery : IQuery<IEnumerable<StockAlertDTO>>;

/// <summary>
/// Get alerts by type.
/// </summary>
public record GetAlertsByTypeQuery(string AlertType) : IQuery<IEnumerable<StockAlertDTO>>;

// ============ Report Queries ============

/// <summary>
/// Get stock valuation report.
/// </summary>
public record GetStockValuationQuery(Guid? WarehouseId = null) : IQuery<IEnumerable<StockValuationDTO>>;

/// <summary>
/// Get stock aging report.
/// </summary>
public record GetStockAgingQuery : IQuery<IEnumerable<StockAgingSummaryDTO>>;

/// <summary>
/// Get inventory turnover report.
/// </summary>
public record GetInventoryTurnoverQuery : IQuery<IEnumerable<InventoryTurnoverDTO>>;

/// <summary>
/// Get dead stock report.
/// </summary>
public record GetDeadStockQuery(int DaysSinceLastMovement = 90) : IQuery<IEnumerable<DeadStockDTO>>;

/// <summary>
/// Get low stock report.
/// </summary>
public record GetLowStockQuery : IQuery<IEnumerable<LowStockReportDTO>>;

/// <summary>
/// Get expiry risk report.
/// </summary>
public record GetExpiryRiskQuery(int DaysThreshold = 30) : IQuery<IEnumerable<ExpiryRiskDTO>>;

/// <summary>
/// Get reorder recommendations.
/// </summary>
public record GetReorderRecommendationsQuery : IQuery<IEnumerable<ReorderRecommendationDTO>>;

/// <summary>
/// Get backorder summary.
/// </summary>
public record GetBackorderSummaryQuery : IQuery<IEnumerable<BackorderSummaryDTO>>;

/// <summary>
/// Get movement history.
/// </summary>
public record GetMovementHistoryQuery(
    Guid? StockItemId = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null
) : IQuery<IEnumerable<StockMovementDTO>>;
