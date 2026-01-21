namespace StockService.Application.Services.Abstractions;

/// <summary>
/// Service for handling idempotent command execution.
/// Prevents duplicate processing of commands with the same idempotency key.
/// </summary>
public interface IIdempotencyService
{
    /// <summary>
    /// Executes an action with idempotency protection.
    /// If the same key was processed recently, returns the cached result.
    /// </summary>
    Task<T> ExecuteAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> action,
        TimeSpan? ttl = null,
        CancellationToken ct = default);

    /// <summary>
    /// Checks if a key has been processed and returns the cached result if available.
    /// </summary>
    Task<(bool Found, T? Result)> TryGetAsync<T>(string key, CancellationToken ct = default);

    /// <summary>
    /// Stores a result for an idempotency key.
    /// </summary>
    Task SetAsync<T>(string key, T result, TimeSpan? ttl = null, CancellationToken ct = default);
}
