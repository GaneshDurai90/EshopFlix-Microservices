namespace StockService.Application.DTOs;

public record StockAdjustmentDTO
{
    public Guid AdjustmentId { get; init; }
    public Guid StockItemId { get; init; }
    public Guid ProductId { get; init; }
    public string? Sku { get; init; }
    public string? WarehouseName { get; init; }
    public string AdjustmentType { get; init; } = string.Empty; // Increase, Decrease, Correction, Damage, Return
    public int AdjustmentQuantity { get; init; }
    public int QuantityBefore { get; init; }
    public int QuantityAfter { get; init; }
    public string? Reason { get; init; }
    public string? Notes { get; init; }
    public Guid? PerformedBy { get; init; }
    public Guid? ApprovedBy { get; init; }
    public DateTime AdjustmentDate { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record CreateAdjustmentRequest
{
    public Guid StockItemId { get; init; }
    public string AdjustmentType { get; init; } = "Correction"; // Increase, Decrease, Correction, Damage, Return, Shrinkage
    public int AdjustmentQuantity { get; init; }
    public string? Reason { get; init; }
    public string? Notes { get; init; }
    public Guid PerformedBy { get; init; }
    public Guid? ApprovedBy { get; init; } // Required for certain adjustment types
}

public record StockMovementDTO
{
    public Guid MovementId { get; init; }
    public Guid StockItemId { get; init; }
    public Guid ProductId { get; init; }
    public string? Sku { get; init; }
    public string? WarehouseName { get; init; }
    public string MovementType { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public int QuantityBefore { get; init; }
    public int QuantityAfter { get; init; }
    public string? ReferenceType { get; init; }
    public Guid? ReferenceId { get; init; }
    public string? Reason { get; init; }
    public string? Notes { get; init; }
    public Guid? PerformedBy { get; init; }
    public DateTime MovementDate { get; init; }
    public DateTime CreatedAt { get; init; }
}
