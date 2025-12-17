using System;
using System.Collections.Generic;
using System.Net;

namespace CatalogService.Application.Exceptions
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

        public static AppException Validation(IDictionary<string, string[]> errors, string? detail = null, string? type = "urn:problem:catalog:validation.failed")
            => new((int)HttpStatusCode.BadRequest,
                   "Validation failed",
                   detail ?? "One or more validation errors occurred.",
                   type,
                   errors);

        public static AppException NotFound(string resource, string message)
            => new((int)HttpStatusCode.NotFound,
                   "Not Found",
                   message,
                   "urn:problem:catalog:notfound",
                   extensions: new Dictionary<string, object?> { ["resource"] = resource });

        public static AppException Business(string code, string message)
            => new((int)HttpStatusCode.UnprocessableEntity,
                   "Business rule violation",
                   message,
                   $"urn:problem:catalog:{code}",
                   extensions: new Dictionary<string, object?> { ["code"] = code });

        public static AppException External(string service, System.Net.HttpStatusCode upstreamStatus, string? responseBody = null)
            => new((int)HttpStatusCode.BadGateway,
                   "Upstream service failure",
                   $"External service '{service}' failed with {(int)upstreamStatus}.",
                   "urn:problem:catalog:external-service",
                   extensions: new Dictionary<string, object?>
                   {
                       ["service"] = service,
                       ["statusCode"] = (int)upstreamStatus,
                       ["response"] = responseBody
                   });
    }
}
