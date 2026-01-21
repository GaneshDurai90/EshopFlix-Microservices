namespace StockService.Application.DTOs;

public record StockReservationDTO
{
    public Guid ReservationId { get; init; }
    public Guid StockItemId { get; init; }
    public Guid? CartId { get; init; }
    public Guid? OrderId { get; init; }
    public Guid? CustomerId { get; init; }
    public int ReservedQuantity { get; init; }
    public string? ReservationStatus { get; init; }
    public DateTime ReservedAt { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public DateTime? ReleasedAt { get; init; }
    public string? ReservationType { get; init; }
    public DateTime UpdatedAt { get; init; }

    // Related data
    public Guid? ProductId { get; init; }
    public string? Sku { get; init; }
    public string? WarehouseName { get; init; }
}

public record CreateReservationRequest
{
    public Guid ProductId { get; init; }
    public Guid? VariationId { get; init; }
    public Guid? WarehouseId { get; init; } // Optional - auto-allocate if not specified
    public Guid? CartId { get; init; }
    public Guid? OrderId { get; init; }
    public Guid? CustomerId { get; init; }
    public int Quantity { get; init; }
    public string ReservationType { get; init; } = "Cart"; // Cart, Order, PreOrder
    public int? TtlMinutes { get; init; } // Time-to-live for reservation
}

public record CreateReservationResponse
{
    public Guid ReservationId { get; init; }
    public Guid StockItemId { get; init; }
    public int ReservedQuantity { get; init; }
    public DateTime ExpiresAt { get; init; }
    public string WarehouseName { get; init; } = string.Empty;
    public bool Success { get; init; }
    public string? Message { get; init; }
}

public record CommitReservationRequest
{
    public Guid ReservationId { get; init; }
    public Guid OrderId { get; init; }
}

public record ReleaseReservationRequest
{
    public Guid ReservationId { get; init; }
    public string? Reason { get; init; }
}
