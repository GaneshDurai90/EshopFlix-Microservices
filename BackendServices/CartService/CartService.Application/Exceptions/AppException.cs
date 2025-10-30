using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CartService.Application.Exceptions
{
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

        // Factory helpers
        public static AppException Validation(IDictionary<string, string[]> errors, string? detail = null, string? type = "urn:problem:cart:validation.failed")
        {
            return new(StatusCodes.Status400BadRequest, "Validation failed", detail ?? "One or more validation errors occurred.", type, errors);
        }

        public static AppException NotFound(string resource, string message)
            => new(StatusCodes.Status404NotFound, "Not Found", message, "urn:problem:cart:notfound",
                   extensions: new Dictionary<string, object?> { ["resource"] = resource });

        public static AppException Business(string code, string message)
            => new(StatusCodes.Status422UnprocessableEntity, "Business rule violation", message, $"urn:problem:cart:{code}",
                   extensions: new Dictionary<string, object?> { ["code"] = code });

        public static AppException External(string service, System.Net.HttpStatusCode upstreamStatus, string? responseBody = null)
            => new(StatusCodes.Status502BadGateway, "Upstream service failure",
                   $"External service '{service}' failed with {(int)upstreamStatus}.",
                   "urn:problem:cart:external-service",
                   extensions: new Dictionary<string, object?>
                   {
                       ["service"] = service,
                       ["statusCode"] = (int)upstreamStatus,
                       ["response"] = responseBody
                   });
    }
}
