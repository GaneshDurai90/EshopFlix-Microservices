using System.ComponentModel.DataAnnotations;

namespace StockService.API.Contracts.Stock;

/// <summary>
/// Request to reserve stock for a cart or order.
/// </summary>
public record ReserveStockRequest
{
    [Required]
    public Guid ProductId { get; init; }
    
    public Guid? VariationId { get; init; }
    
    public Guid? WarehouseId { get; init; }
    
    public Guid? CartId { get; init; }
    
    public Guid? OrderId { get; init; }
    
    public Guid? CustomerId { get; init; }
    
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
    public int Quantity { get; init; }
    
    public string ReservationType { get; init; } = "Cart";
    
    [Range(1, 1440, ErrorMessage = "TTL must be between 1 and 1440 minutes")]
    public int? TtlMinutes { get; init; }
}

/// <summary>
/// Request to commit a reservation to an order.
/// </summary>
public record CommitReservationRequest
{
    [Required]
    public Guid ReservationId { get; init; }
    
    [Required]
    public Guid OrderId { get; init; }
}

/// <summary>
/// Request to release a reservation.
/// </summary>
public record ReleaseReservationRequest
{
    [Required]
    public Guid ReservationId { get; init; }
    
    public string? Reason { get; init; }
}
