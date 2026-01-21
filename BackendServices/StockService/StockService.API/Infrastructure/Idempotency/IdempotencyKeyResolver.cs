using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace StockService.API.Infrastructure.Idempotency;

/// <summary>
/// Resolves idempotency keys from request headers or generates deterministic keys.
/// Ensures repeated requests with the same key are handled only once.
/// </summary>
public static class IdempotencyKeyResolver
{
    private static readonly string[] IdempotencyHeaders = { "x-idempotency-key", "Idempotency-Key" };

    /// <summary>
    /// Resolves an idempotency key from request headers or generates one from the request body.
    /// </summary>
    /// <param name="request">The HTTP request</param>
    /// <param name="body">Optional request body for hash generation</param>
    /// <returns>A tuple containing the key and optionally the derived hash</returns>
    public static (string key, string? derivedHash) Resolve(HttpRequest request, object? body = null)
    {
        // Check for explicit idempotency key in headers
        foreach (var headerName in IdempotencyHeaders)
        {
            var headerKey = request.Headers[headerName].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(headerKey))
            {
                return (headerKey!, null);
            }
        }

        // Generate deterministic hash from method, path, and body
        var payload = body is null ? string.Empty : JsonSerializer.Serialize(body);
        using var sha = SHA256.Create();
        var input = $"{request.Method}:{request.Path}|{payload}";
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        var hash = Convert.ToHexString(bytes);
        return (hash, hash);
    }

    /// <summary>
    /// Generates an idempotency key from specific operation parameters.
    /// </summary>
    public static string GenerateKey(string operation, params object[] parameters)
    {
        var input = $"{operation}:{string.Join(":", parameters)}";
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }

    /// <summary>
    /// Extracts the idempotency key from request headers only.
    /// </summary>
    public static string? GetFromHeaders(HttpRequest request)
    {
        foreach (var headerName in IdempotencyHeaders)
        {
            var headerKey = request.Headers[headerName].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(headerKey))
            {
                return headerKey;
            }
        }
        return null;
    }
}
