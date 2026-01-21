namespace StockService.Application.DTOs;

public record StockTransferDTO
{
    public Guid TransferId { get; init; }
    public Guid FromWarehouseId { get; init; }
    public string? FromWarehouseName { get; init; }
    public Guid ToWarehouseId { get; init; }
    public string? ToWarehouseName { get; init; }
    public string TransferStatus { get; init; } = string.Empty;
    public Guid? RequestedBy { get; init; }
    public DateTime? EstimatedArrival { get; init; }
    public DateTime? ShippedAt { get; init; }
    public DateTime? ReceivedAt { get; init; }
    public string? TrackingNumber { get; init; }
    public string? Notes { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public List<StockTransferItemDTO> Items { get; init; } = new();
}

public record StockTransferItemDTO
{
    public Guid TransferItemId { get; init; }
    public Guid StockItemId { get; init; }
    public Guid ProductId { get; init; }
    public string? Sku { get; init; }
    public int RequestedQuantity { get; init; }
    public int? ShippedQuantity { get; init; }
    public int? ReceivedQuantity { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record CreateTransferRequest
{
    public Guid FromWarehouseId { get; init; }
    public Guid ToWarehouseId { get; init; }
    public Guid RequestedBy { get; init; }
    public string? Notes { get; init; }
    public List<TransferItemRequest> Items { get; init; } = new();
}

public record TransferItemRequest
{
    public Guid StockItemId { get; init; }
    public int Quantity { get; init; }
}

public record ShipTransferRequest
{
    public Guid TransferId { get; init; }
    public string? TrackingNumber { get; init; }
    public DateTime? EstimatedArrival { get; init; }
    public List<ShipItemRequest>? Items { get; init; }
}

public record ShipItemRequest
{
    public Guid TransferItemId { get; init; }
    public int ShippedQuantity { get; init; }
}

public record ReceiveTransferRequest
{
    public Guid TransferId { get; init; }
    public List<ReceiveItemRequest>? Items { get; init; }
}

public record ReceiveItemRequest
{
    public Guid TransferItemId { get; init; }
    public int ReceivedQuantity { get; init; }
}
