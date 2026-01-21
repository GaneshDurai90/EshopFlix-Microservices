namespace StockService.Application.DTOs;

public record WarehouseDTO
{
    public Guid WarehouseId { get; init; }
    public string WarehouseName { get; init; } = string.Empty;
    public string WarehouseCode { get; init; } = string.Empty;
    public string? Address { get; init; }
    public string Type { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public int Priority { get; init; }
    public int? Capacity { get; init; }
    public string? ContactDetails { get; init; }
    public string? OperatingHours { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }

    // Summary data
    public int? TotalStockItems { get; init; }
    public int? TotalQuantity { get; init; }
    public decimal? TotalValue { get; init; }
}

public record CreateWarehouseRequest
{
    public string WarehouseName { get; init; } = string.Empty;
    public string WarehouseCode { get; init; } = string.Empty;
    public string? Address { get; init; }
    public string Type { get; init; } = "Standard"; // Standard, DropShip, Consignment
    public int Priority { get; init; } = 1;
    public int? Capacity { get; init; }
    public string? ContactDetails { get; init; }
    public string? OperatingHours { get; init; }
}

public record UpdateWarehouseRequest
{
    public Guid WarehouseId { get; init; }
    public string? WarehouseName { get; init; }
    public string? Address { get; init; }
    public bool? IsActive { get; init; }
    public int? Priority { get; init; }
    public int? Capacity { get; init; }
    public string? ContactDetails { get; init; }
    public string? OperatingHours { get; init; }
}
