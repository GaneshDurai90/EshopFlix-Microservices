using System.Net;

namespace StockService.Application.Exceptions;

/// <summary>
/// Application exception for Stock Service business and validation errors.
/// Maps to RFC 7807 Problem Details responses.
/// </summary>
public sealed class AppException : Exception
{
    public int StatusCode { get; }
    public string Title { get; }
    public string? Type { get; }
    public IDictionary<string, string[]>? Errors { get; }
    public IDictionary<string, object?>? Extensions { get; }

    public AppException(
        int statusCode,
        string title,
        string message,
        string? type = null,
        IDictionary<string, string[]>? errors = null,
        IDictionary<string, object?>? extensions = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        Title = title;
        Type = type;
        Errors = errors;
        Extensions = extensions;
    }

    // ============ Factory Methods ============

    /// <summary>
    /// Creates a validation exception with field-level errors.
    /// </summary>
    public static AppException Validation(
        IDictionary<string, string[]> errors, 
        string? detail = null, 
        string? type = "urn:problem:stock:validation.failed")
        => new(
            (int)HttpStatusCode.BadRequest,
            "Validation failed",
            detail ?? "One or more validation errors occurred.",
            type,
            errors);

    /// <summary>
    /// Creates a not found exception.
    /// </summary>
    public static AppException NotFound(string resource, string message)
        => new(
            (int)HttpStatusCode.NotFound,
            "Not Found",
            message,
            "urn:problem:stock:notfound",
            extensions: new Dictionary<string, object?> { ["resource"] = resource });

    /// <summary>
    /// Creates a not found exception for a specific entity.
    /// </summary>
    public static AppException NotFound<T>(object id)
        => new(
            (int)HttpStatusCode.NotFound,
            "Not Found",
            $"{typeof(T).Name} with ID '{id}' was not found.",
            "urn:problem:stock:notfound",
            extensions: new Dictionary<string, object?> 
            { 
                ["resource"] = typeof(T).Name,
                ["id"] = id?.ToString()
            });

    /// <summary>
    /// Creates a business rule violation exception.
    /// </summary>
    public static AppException Business(string code, string message)
        => new(
            (int)HttpStatusCode.UnprocessableEntity,
            "Business rule violation",
            message,
            $"urn:problem:stock:{code}",
            extensions: new Dictionary<string, object?> { ["code"] = code });

    /// <summary>
    /// Creates an insufficient stock exception.
    /// </summary>
    public static AppException InsufficientStock(Guid productId, int requested, int available)
        => new(
            (int)HttpStatusCode.Conflict,
            "Insufficient stock",
            $"Requested quantity ({requested}) exceeds available stock ({available}).",
            "urn:problem:stock:insufficient-stock",
            extensions: new Dictionary<string, object?>
            {
                ["productId"] = productId,
                ["requestedQuantity"] = requested,
                ["availableQuantity"] = available
            });

    /// <summary>
    /// Creates a reservation conflict exception.
    /// </summary>
    public static AppException ReservationConflict(Guid reservationId, string reason)
        => new(
            (int)HttpStatusCode.Conflict,
            "Reservation conflict",
            reason,
            "urn:problem:stock:reservation-conflict",
            extensions: new Dictionary<string, object?>
            {
                ["reservationId"] = reservationId
            });

    /// <summary>
    /// Creates an expired reservation exception.
    /// </summary>
    public static AppException ReservationExpired(Guid reservationId)
        => new(
            (int)HttpStatusCode.Gone,
            "Reservation expired",
            $"Reservation {reservationId} has expired and is no longer valid.",
            "urn:problem:stock:reservation-expired",
            extensions: new Dictionary<string, object?>
            {
                ["reservationId"] = reservationId
            });

    /// <summary>
    /// Creates an external service failure exception.
    /// </summary>
    public static AppException External(string service, HttpStatusCode upstreamStatus, string? responseBody = null)
        => new(
            (int)HttpStatusCode.BadGateway,
            "Upstream service failure",
            $"External service '{service}' failed with {(int)upstreamStatus}.",
            "urn:problem:stock:external-service",
            extensions: new Dictionary<string, object?>
            {
                ["service"] = service,
                ["statusCode"] = (int)upstreamStatus,
                ["response"] = responseBody
            });

    /// <summary>
    /// Creates a duplicate request exception (idempotency violation).
    /// </summary>
    public static AppException DuplicateRequest(string idempotencyKey)
        => new(
            (int)HttpStatusCode.Conflict,
            "Duplicate request",
            "This request has already been processed.",
            "urn:problem:stock:duplicate-request",
            extensions: new Dictionary<string, object?>
            {
                ["idempotencyKey"] = idempotencyKey
            });

    /// <summary>
    /// Creates a concurrent modification exception.
    /// </summary>
    public static AppException ConcurrentModification(string resource)
        => new(
            (int)HttpStatusCode.Conflict,
            "Concurrent modification",
            $"The {resource} was modified by another request. Please retry.",
            "urn:problem:stock:concurrency-conflict",
            extensions: new Dictionary<string, object?>
            {
                ["resource"] = resource
            });
}
