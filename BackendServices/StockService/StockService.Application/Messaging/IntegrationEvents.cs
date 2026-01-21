namespace StockService.Application.Messaging;

/// <summary>
/// Base interface for all integration events.
/// </summary>
public interface IIntegrationEvent
{
    Guid EventId { get; }
    DateTime OccurredAt { get; }
    string EventType { get; }
}

/// <summary>
/// Base record for integration events with common properties.
/// </summary>
public abstract record IntegrationEventBase : IIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public abstract string EventType { get; }
}

// ============ Stock Reservation Events ============

/// <summary>
/// Published when stock is reserved for a cart or order.
/// </summary>
public record StockReservedEvent(
    Guid ReservationId,
    Guid ProductId,
    Guid? VariationId,
    Guid StockItemId,
    int Quantity,
    Guid? CartId,
    Guid? OrderId,
    DateTime ExpiresAt
) : IntegrationEventBase
{
    public override string EventType => "Stock.Reserved";
}

/// <summary>
/// Published when a reservation is committed to an order.
/// </summary>
public record StockCommittedEvent(
    Guid ReservationId,
    Guid OrderId,
    Guid StockItemId,
    int Quantity
) : IntegrationEventBase
{
    public override string EventType => "Stock.Committed";
}

/// <summary>
/// Published when a reservation is released.
/// </summary>
public record StockReleasedEvent(
    Guid ReservationId,
    Guid StockItemId,
    int Quantity,
    string Reason
) : IntegrationEventBase
{
    public override string EventType => "Stock.Released";
}

// ============ Stock Adjustment Events ============

/// <summary>
/// Published when stock quantity is adjusted.
/// </summary>
public record StockAdjustedEvent(
    Guid StockItemId,
    Guid ProductId,
    string AdjustmentType,
    int AdjustmentQuantity,
    int QuantityBefore,
    int QuantityAfter,
    string? Reason,
    Guid PerformedBy
) : IntegrationEventBase
{
    public override string EventType => "Stock.Adjusted";
}

/// <summary>
/// Published when a new stock item is created.
/// </summary>
public record StockItemCreatedEvent(
    Guid StockItemId,
    Guid ProductId,
    Guid? VariationId,
    Guid WarehouseId,
    int InitialQuantity,
    string? Sku
) : IntegrationEventBase
{
    public override string EventType => "Stock.ItemCreated";
}

// ============ Alert Events ============

/// <summary>
/// Published when stock falls below minimum level.
/// </summary>
public record LowStockAlertEvent(
    Guid StockItemId,
    Guid ProductId,
    string? Sku,
    Guid WarehouseId,
    int CurrentQuantity,
    int MinimumLevel
) : IntegrationEventBase
{
    public override string EventType => "Stock.LowStockAlert";
}

/// <summary>
/// Published when stock exceeds maximum level.
/// </summary>
public record OverStockAlertEvent(
    Guid StockItemId,
    Guid ProductId,
    string? Sku,
    Guid WarehouseId,
    int CurrentQuantity,
    int MaximumLevel
) : IntegrationEventBase
{
    public override string EventType => "Stock.OverStockAlert";
}

/// <summary>
/// Published when stock is about to expire.
/// </summary>
public record ExpiryAlertEvent(
    Guid StockItemId,
    Guid ProductId,
    string? Sku,
    string? BatchNumber,
    Guid WarehouseId,
    int Quantity,
    DateOnly ExpiryDate,
    int DaysUntilExpiry
) : IntegrationEventBase
{
    public override string EventType => "Stock.ExpiryAlert";
}

/// <summary>
/// Published when stock goes out of stock.
/// </summary>
public record OutOfStockEvent(
    Guid ProductId,
    Guid? VariationId,
    string? Sku,
    Guid WarehouseId
) : IntegrationEventBase
{
    public override string EventType => "Stock.OutOfStock";
}

/// <summary>
/// Published when stock is back in stock.
/// </summary>
public record BackInStockEvent(
    Guid ProductId,
    Guid? VariationId,
    string? Sku,
    Guid WarehouseId,
    int AvailableQuantity
) : IntegrationEventBase
{
    public override string EventType => "Stock.BackInStock";
}

// ============ Transfer Events ============

/// <summary>
/// Published when a stock transfer is created.
/// </summary>
public record StockTransferCreatedEvent(
    Guid TransferId,
    Guid FromWarehouseId,
    Guid ToWarehouseId,
    int TotalItems,
    Guid RequestedBy
) : IntegrationEventBase
{
    public override string EventType => "Stock.TransferCreated";
}

/// <summary>
/// Published when a stock transfer is completed.
/// </summary>
public record StockTransferCompletedEvent(
    Guid TransferId,
    Guid FromWarehouseId,
    Guid ToWarehouseId,
    int TotalItemsReceived
) : IntegrationEventBase
{
    public override string EventType => "Stock.TransferCompleted";
}

// ============ Catalog Sync Events (for CatalogService) ============

/// <summary>
/// Published to sync availability status to CatalogService.
/// </summary>
public record ProductAvailabilityChangedEvent(
    Guid ProductId,
    Guid? VariationId,
    bool IsInStock,
    int TotalAvailable,
    int TotalReserved
) : IntegrationEventBase
{
    public override string EventType => "Stock.AvailabilityChanged";
}
