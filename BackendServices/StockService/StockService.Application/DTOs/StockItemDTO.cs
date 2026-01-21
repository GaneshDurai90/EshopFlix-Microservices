namespace StockService.Application.DTOs;

public record StockItemDTO
{
    public Guid StockItemId { get; init; }
    public Guid ProductId { get; init; }
    public Guid? VariationId { get; init; }
    public Guid WarehouseId { get; init; }
    public string? WarehouseName { get; init; }
    public string? Sku { get; init; }
    public int AvailableQuantity { get; init; }
    public int ReservedQuantity { get; init; }
    public int InTransitQuantity { get; init; }
    public int DamagedQuantity { get; init; }
    public int? MinimumStockLevel { get; init; }
    public int? MaximumStockLevel { get; init; }
    public int? ReorderQuantity { get; init; }
    public decimal? UnitCost { get; init; }
    public DateTime? LastRestockedAt { get; init; }
    public DateOnly? ExpiryDate { get; init; }
    public string? BatchNumber { get; init; }
    public string? BinLocation { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }

    // Computed properties
    public int TotalQuantity => AvailableQuantity + ReservedQuantity + InTransitQuantity;
    public bool IsLowStock => MinimumStockLevel.HasValue && AvailableQuantity <= MinimumStockLevel.Value;
    public bool IsOverStock => MaximumStockLevel.HasValue && AvailableQuantity >= MaximumStockLevel.Value;
    public decimal? TotalValue => UnitCost.HasValue ? UnitCost.Value * AvailableQuantity : null;
}

public record StockItemSummaryDTO
{
    public Guid ProductId { get; init; }
    public string? Sku { get; init; }
    public int TotalAvailable { get; init; }
    public int TotalReserved { get; init; }
    public int TotalInTransit { get; init; }
    public int WarehouseCount { get; init; }
    public decimal? TotalValue { get; init; }
    public bool IsLowStock { get; init; }
}
