namespace eShopFlix.Web.Models.Stock;

/// <summary>
/// Stock availability information for a product.
/// </summary>
public class StockAvailabilityModel
{
    public Guid ProductId { get; set; }
    public Guid? VariationId { get; set; }
    public int TotalAvailable { get; set; }
    public int TotalReserved { get; set; }
    public int TotalInTransit { get; set; }
    public bool IsInStock { get; set; }
    public bool IsLowStock { get; set; }
    public int WarehouseCount { get; set; }
    public DateTime? NextRestockDate { get; set; }
    
    /// <summary>
    /// Get stock status display text.
    /// </summary>
    public string StatusText => IsInStock
        ? (IsLowStock ? "Low Stock" : "In Stock")
        : "Out of Stock";

    /// <summary>
    /// Get Bootstrap badge class for status.
    /// </summary>
    public string StatusBadgeClass => IsInStock
        ? (IsLowStock ? "bg-warning text-dark" : "bg-success")
        : "bg-danger";
}

/// <summary>
/// Result of checking availability for a specific quantity.
/// </summary>
public class CheckAvailabilityResultModel
{
    public Guid ProductId { get; set; }
    public int RequestedQuantity { get; set; }
    public int AvailableQuantity { get; set; }
    public bool IsAvailable { get; set; }
    public bool CanPartialFulfill { get; set; }
    public IReadOnlyList<AllocationSuggestionModel> Allocations { get; set; } = Array.Empty<AllocationSuggestionModel>();
}

/// <summary>
/// Allocation suggestion from a warehouse.
/// </summary>
public class AllocationSuggestionModel
{
    public Guid StockItemId { get; set; }
    public Guid WarehouseId { get; set; }
    public string? WarehouseName { get; set; }
    public int AllocatedQuantity { get; set; }
    public int Priority { get; set; }
}

/// <summary>
/// Result of a stock reservation request.
/// </summary>
public class ReservationResultModel
{
    public Guid? ReservationId { get; set; }
    public Guid? StockItemId { get; set; }
    public int ReservedQuantity { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? WarehouseName { get; set; }
    public bool Success { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// A stock reservation for a cart item.
/// </summary>
public class CartReservationModel
{
    public Guid ReservationId { get; set; }
    public Guid StockItemId { get; set; }
    public Guid? ProductId { get; set; }
    public string? Sku { get; set; }
    public int ReservedQuantity { get; set; }
    public string? ReservationStatus { get; set; }
    public DateTime ReservedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? WarehouseName { get; set; }
    
    /// <summary>
    /// Check if reservation is still valid.
    /// </summary>
    public bool IsValid => ReservationStatus == "Pending" && 
                           (!ExpiresAt.HasValue || ExpiresAt.Value > DateTime.UtcNow);
    
    /// <summary>
    /// Time remaining before expiration.
    /// </summary>
    public TimeSpan? TimeRemaining => ExpiresAt.HasValue 
        ? ExpiresAt.Value - DateTime.UtcNow 
        : null;
}

/// <summary>
/// Stock summary for display on product cards.
/// </summary>
public class StockSummaryModel
{
    public int ProductId { get; set; }
    public bool IsInStock { get; set; }
    public bool IsLowStock { get; set; }
    public int AvailableQuantity { get; set; }
    
    public string StatusText => IsInStock
        ? (IsLowStock ? "Only a few left" : "In Stock")
        : "Out of Stock";
        
    public string StatusClass => IsInStock
        ? (IsLowStock ? "text-warning" : "text-success")
        : "text-danger";
}
