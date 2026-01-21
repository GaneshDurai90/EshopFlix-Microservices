namespace StockService.Application.DTOs;

public record StockAlertDTO
{
    public Guid AlertId { get; init; }
    public Guid? StockItemId { get; init; }
    public Guid? ProductId { get; init; }
    public string? Sku { get; init; }
    public string? WarehouseName { get; init; }
    public string AlertType { get; init; } = string.Empty; // LowStock, OutOfStock, OverStock, Expiry, Backorder
    public string AlertStatus { get; init; } = string.Empty; // Active, Acknowledged, Resolved
    public string? Message { get; init; }
    public DateTime TriggeredAt { get; init; }
    public DateTime? AcknowledgedAt { get; init; }
    public Guid? AcknowledgedBy { get; init; }
    public DateTime? ResolvedAt { get; init; }
}

public record AcknowledgeAlertRequest
{
    public Guid AlertId { get; init; }
    public Guid AcknowledgedBy { get; init; }
}

// ============ Availability DTOs ============

public record StockAvailabilityDTO
{
    public Guid ProductId { get; init; }
    public Guid? VariationId { get; init; }
    public string? Sku { get; init; }
    public int TotalAvailable { get; init; }
    public int TotalReserved { get; init; }
    public bool IsInStock { get; init; }
    public bool IsLowStock { get; init; }
    public List<WarehouseStockDTO> WarehouseBreakdown { get; init; } = new();
}

public record WarehouseStockDTO
{
    public Guid WarehouseId { get; init; }
    public string WarehouseName { get; init; } = string.Empty;
    public string WarehouseCode { get; init; } = string.Empty;
    public int AvailableQuantity { get; init; }
    public int ReservedQuantity { get; init; }
    public int Priority { get; init; }
}

/// <summary>
/// Application-level request for availability check.
/// Used by repositories and services.
/// </summary>
public record CheckAvailabilityRequest
{
    public Guid ProductId { get; init; }
    public Guid? VariationId { get; init; }
    public int Quantity { get; init; } = 1;
    public Guid? PreferredWarehouseId { get; init; }
}

public record CheckAvailabilityResponse
{
    public bool IsAvailable { get; init; }
    public int AvailableQuantity { get; init; }
    public int RequestedQuantity { get; init; }
    public List<AllocationSuggestionDTO> Allocations { get; init; } = new();
    public string? Message { get; init; }
}

public record AllocationSuggestionDTO
{
    public Guid WarehouseId { get; init; }
    public string WarehouseName { get; init; } = string.Empty;
    public Guid StockItemId { get; init; }
    public int AllocatedQuantity { get; init; }
    public int Priority { get; init; }
}
