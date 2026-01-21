namespace StockService.Application.DTOs;

// Reports and Analytics DTOs
public record StockValuationDTO
{
    public Guid ProductId { get; init; }
    public Guid? VariationId { get; init; }
    public Guid WarehouseId { get; init; }
    public string? WarehouseName { get; init; }
    public string? Sku { get; init; }
    public int Quantity { get; init; }
    public decimal? UnitCost { get; init; }
    public decimal? TotalValue { get; init; }
}

public record StockAgingSummaryDTO
{
    public string AgeBucket { get; init; } = string.Empty; // 0-30, 31-60, 61-90, 90+
    public int ItemCount { get; init; }
    public int TotalQuantity { get; init; }
    public decimal TotalValue { get; init; }
}

public record InventoryTurnoverDTO
{
    public Guid ProductId { get; init; }
    public string? Sku { get; init; }
    public decimal TurnoverRatio { get; init; }
    public decimal DaysOfInventory { get; init; }
    public int TotalSold { get; init; }
    public int AverageInventory { get; init; }
}

public record DeadStockDTO
{
    public Guid StockItemId { get; init; }
    public Guid ProductId { get; init; }
    public string? Sku { get; init; }
    public Guid WarehouseId { get; init; }
    public string? WarehouseName { get; init; }
    public int Quantity { get; init; }
    public decimal? Value { get; init; }
    public int DaysSinceLastMovement { get; init; }
    public DateTime? LastMovementDate { get; init; }
}

public record LowStockReportDTO
{
    public Guid StockItemId { get; init; }
    public Guid ProductId { get; init; }
    public string? Sku { get; init; }
    public Guid WarehouseId { get; init; }
    public string? WarehouseName { get; init; }
    public int AvailableQuantity { get; init; }
    public int MinimumStockLevel { get; init; }
    public int? ReorderQuantity { get; init; }
    public int ShortfallQuantity { get; init; }
}

public record ExpiryRiskDTO
{
    public Guid StockItemId { get; init; }
    public Guid ProductId { get; init; }
    public string? Sku { get; init; }
    public string? BatchNumber { get; init; }
    public Guid WarehouseId { get; init; }
    public string? WarehouseName { get; init; }
    public int Quantity { get; init; }
    public DateOnly ExpiryDate { get; init; }
    public int DaysUntilExpiry { get; init; }
    public string RiskLevel { get; init; } = string.Empty; // Critical, High, Medium, Low
}

public record ReorderRecommendationDTO
{
    public Guid ProductId { get; init; }
    public string? Sku { get; init; }
    public Guid? PreferredSupplierId { get; init; }
    public string? SupplierName { get; init; }
    public int CurrentStock { get; init; }
    public int MinimumLevel { get; init; }
    public int RecommendedQuantity { get; init; }
    public decimal? EstimatedCost { get; init; }
    public int? LeadTimeDays { get; init; }
}

public record BackorderSummaryDTO
{
    public Guid ProductId { get; init; }
    public string? Sku { get; init; }
    public int TotalBackorderedQuantity { get; init; }
    public int PendingCount { get; init; }
    public DateTime? OldestBackorderDate { get; init; }
    public int? EstimatedFulfillmentDays { get; init; }
}
