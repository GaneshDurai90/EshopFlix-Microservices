using System.ComponentModel.DataAnnotations;

namespace StockService.API.Contracts.Stock;

/// <summary>
/// Request to increase stock quantity.
/// </summary>
public record IncreaseStockRequest
{
    [Required]
    public Guid StockItemId { get; init; }
    
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
    public int Quantity { get; init; }
    
    [Required]
    [StringLength(500, MinimumLength = 1)]
    public string Reason { get; init; } = string.Empty;
    
    [Required]
    public Guid PerformedBy { get; init; }
}

/// <summary>
/// Request to decrease stock quantity.
/// </summary>
public record DecreaseStockRequest
{
    [Required]
    public Guid StockItemId { get; init; }
    
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
    public int Quantity { get; init; }
    
    [Required]
    [StringLength(500, MinimumLength = 1)]
    public string Reason { get; init; } = string.Empty;
    
    [Required]
    public Guid PerformedBy { get; init; }
}

/// <summary>
/// Request for manual stock adjustment.
/// </summary>
public record CreateAdjustmentRequest
{
    [Required]
    public Guid StockItemId { get; init; }
    
    [Required]
    [StringLength(50)]
    public string AdjustmentType { get; init; } = string.Empty;
    
    [Required]
    public int AdjustmentQuantity { get; init; }
    
    [StringLength(500)]
    public string? Reason { get; init; }
    
    [StringLength(2000)]
    public string? Notes { get; init; }
    
    [Required]
    public Guid PerformedBy { get; init; }
    
    public Guid? ApprovedBy { get; init; }
}

/// <summary>
/// Request to create a new stock item.
/// </summary>
public record CreateStockItemRequest
{
    [Required]
    public Guid ProductId { get; init; }
    
    public Guid? VariationId { get; init; }
    
    [Required]
    public Guid WarehouseId { get; init; }
    
    [StringLength(100)]
    public string? Sku { get; init; }
    
    [Range(0, int.MaxValue)]
    public int InitialQuantity { get; init; }
    
    [Range(0, int.MaxValue)]
    public int? MinimumStockLevel { get; init; }
    
    [Range(0, int.MaxValue)]
    public int? MaximumStockLevel { get; init; }
    
    [Range(0, int.MaxValue)]
    public int? ReorderQuantity { get; init; }
    
    [Range(0, double.MaxValue)]
    public decimal? UnitCost { get; init; }
    
    public DateOnly? ExpiryDate { get; init; }
    
    [StringLength(100)]
    public string? BatchNumber { get; init; }
    
    [StringLength(100)]
    public string? BinLocation { get; init; }
}

/// <summary>
/// Request to update stock item settings.
/// </summary>
public record UpdateStockItemRequest
{
    [Required]
    public Guid StockItemId { get; init; }
    
    [Range(0, int.MaxValue)]
    public int? MinimumStockLevel { get; init; }
    
    [Range(0, int.MaxValue)]
    public int? MaximumStockLevel { get; init; }
    
    [Range(0, int.MaxValue)]
    public int? ReorderQuantity { get; init; }
    
    [Range(0, double.MaxValue)]
    public decimal? UnitCost { get; init; }
    
    [StringLength(100)]
    public string? BinLocation { get; init; }
    
    public bool? IsActive { get; init; }
}
