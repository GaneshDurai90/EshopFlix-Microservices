using System.ComponentModel.DataAnnotations;

namespace StockService.API.Contracts.Alert;

/// <summary>
/// Request to acknowledge an alert.
/// </summary>
public record AcknowledgeAlertRequest
{
    [Required]
    public Guid AlertId { get; init; }
    
    [Required]
    public Guid AcknowledgedBy { get; init; }
    
    [StringLength(500)]
    public string? Notes { get; init; }
}

/// <summary>
/// Request to create a custom alert.
/// </summary>
public record CreateAlertRequest
{
    [Required]
    public Guid StockItemId { get; init; }
    
    [Required]
    [StringLength(50)]
    public string AlertType { get; init; } = string.Empty;
    
    [Required]
    [StringLength(500)]
    public string Message { get; init; } = string.Empty;
    
    [StringLength(50)]
    public string Severity { get; init; } = "Warning";
}
