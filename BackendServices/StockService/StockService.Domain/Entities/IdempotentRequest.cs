namespace StockService.Domain.Entities;

/// <summary>
/// Entity for tracking idempotent requests to prevent duplicate processing.
/// </summary>
public class IdempotentRequest
{
    public long Id { get; set; }
    
    /// <summary>
    /// Unique key identifying the request (e.g., idempotency key from header or generated).
    /// </summary>
    public string Key { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional user ID associated with the request.
    /// </summary>
    public long? UserId { get; set; }
    
    /// <summary>
    /// Hash of the request payload to detect different requests with same key.
    /// </summary>
    public string? RequestHash { get; set; }
    
    /// <summary>
    /// HTTP status code of the response.
    /// </summary>
    public int? StatusCode { get; set; }
    
    /// <summary>
    /// JSON serialized response body.
    /// </summary>
    public string? ResponseBody { get; set; }
    
    /// <summary>
    /// When the request was first received.
    /// </summary>
    public DateTime CreatedOn { get; set; }
    
    /// <summary>
    /// When this idempotency record expires and can be removed.
    /// </summary>
    public DateTime? ExpiresOn { get; set; }
    
    /// <summary>
    /// Lock timestamp to prevent concurrent processing.
    /// </summary>
    public DateTime? LockedUntil { get; set; }
}
