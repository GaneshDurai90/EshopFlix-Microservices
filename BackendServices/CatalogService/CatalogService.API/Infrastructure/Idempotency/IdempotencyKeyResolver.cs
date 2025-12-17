using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace CatalogService.API.Infrastructure.Idempotency
{
    public static class IdempotencyKeyResolver
    {
        public static (string key, string? derivedHash) Resolve(HttpRequest request, object? body = null)
        {
            var headerKey = request.Headers["x-idempotency-key"].FirstOrDefault()
                           ?? request.Headers["Idempotency-Key"].FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(headerKey))
            {
                return (headerKey!, null);
            }

            var payload = body is null ? string.Empty : JsonSerializer.Serialize(body);
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes($"{request.Method}:{request.Path}|{payload}"));
            var hash = Convert.ToHexString(bytes);
            return (hash, hash);
        }
    }
}
