namespace StockService.Application.DTOs;

// Purchase Order DTOs
public record PurchaseOrderDTO
{
    public Guid PurchaseOrderId { get; init; }
    public string Ponumber { get; init; } = string.Empty;
    public Guid SupplierId { get; init; }
    public string? SupplierName { get; init; }
    public Guid WarehouseId { get; init; }
    public string? WarehouseName { get; init; }
    public string OrderStatus { get; init; } = string.Empty;
    public DateTime OrderDate { get; init; }
    public DateTime? ExpectedDeliveryDate { get; init; }
    public DateTime? ReceivedDate { get; init; }
    public decimal? TotalAmount { get; init; }
    public string? Notes { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public List<PurchaseOrderItemDTO> Items { get; init; } = new();
}

public record PurchaseOrderItemDTO
{
    public Guid PoitemId { get; init; }
    public Guid ProductId { get; init; }
    public Guid? VariationId { get; init; }
    public string? Sku { get; init; }
    public int OrderedQuantity { get; init; }
    public int? ReceivedQuantity { get; init; }
    public decimal UnitCost { get; init; }
    public decimal TotalCost { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record CreatePurchaseOrderRequest
{
    public Guid SupplierId { get; init; }
    public Guid WarehouseId { get; init; }
    public DateTime? ExpectedDeliveryDate { get; init; }
    public string? Notes { get; init; }
    public List<CreatePurchaseOrderItemRequest> Items { get; init; } = new();
}

public record CreatePurchaseOrderItemRequest
{
    public Guid ProductId { get; init; }
    public Guid? VariationId { get; init; }
    public string? Sku { get; init; }
    public int OrderedQuantity { get; init; }
    public decimal UnitCost { get; init; }
}

public record ReceivePurchaseOrderRequest
{
    public Guid PurchaseOrderId { get; init; }
    public List<ReceivePurchaseOrderItemRequest> Items { get; init; } = new();
    public Guid PerformedBy { get; init; }
}

public record ReceivePurchaseOrderItemRequest
{
    public Guid PoitemId { get; init; }
    public int ReceivedQuantity { get; init; }
    public string? BatchNumber { get; init; }
    public DateOnly? ExpiryDate { get; init; }
    public string? BinLocation { get; init; }
}

// Supplier DTOs
public record SupplierDTO
{
    public Guid SupplierId { get; init; }
    public string SupplierCode { get; init; } = string.Empty;
    public string SupplierName { get; init; } = string.Empty;
    public string? ContactPerson { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? Address { get; init; }
    public int? LeadTimeDays { get; init; }
    public decimal? Rating { get; init; }
    public string? PaymentTerms { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public record CreateSupplierRequest
{
    public string SupplierCode { get; init; } = string.Empty;
    public string SupplierName { get; init; } = string.Empty;
    public string? ContactPerson { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? Address { get; init; }
    public int? LeadTimeDays { get; init; }
    public string? PaymentTerms { get; init; }
}
