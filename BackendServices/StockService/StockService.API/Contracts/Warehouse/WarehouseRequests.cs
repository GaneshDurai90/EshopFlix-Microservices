using System.ComponentModel.DataAnnotations;

namespace StockService.API.Contracts.Warehouse;

/// <summary>
/// Request to create a new warehouse.
/// </summary>
public record CreateWarehouseRequest
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string WarehouseName { get; init; } = string.Empty;
    
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string WarehouseCode { get; init; } = string.Empty;
    
    [StringLength(500)]
    public string? Address { get; init; }
    
    [Required]
    [StringLength(50)]
    public string Type { get; init; } = "Standard";
    
    [Range(1, 100)]
    public int Priority { get; init; } = 1;
    
    [Range(0, int.MaxValue)]
    public int? Capacity { get; init; }
    
    [StringLength(500)]
    public string? ContactDetails { get; init; }
    
    [StringLength(200)]
    public string? OperatingHours { get; init; }
}

/// <summary>
/// Request to update warehouse settings.
/// </summary>
public record UpdateWarehouseRequest
{
    [Required]
    public Guid WarehouseId { get; init; }
    
    [StringLength(200, MinimumLength = 1)]
    public string? WarehouseName { get; init; }
    
    [StringLength(500)]
    public string? Address { get; init; }
    
    public bool? IsActive { get; init; }
    
    [Range(1, 100)]
    public int? Priority { get; init; }
    
    [Range(0, int.MaxValue)]
    public int? Capacity { get; init; }
    
    [StringLength(500)]
    public string? ContactDetails { get; init; }
    
    [StringLength(200)]
    public string? OperatingHours { get; init; }
}
