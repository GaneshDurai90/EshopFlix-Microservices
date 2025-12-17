using System;

namespace CatalogService.Domain.Entities
{
    public class IdempotentRequest
    {
        public long Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public long? UserId { get; set; }
        public string? RequestHash { get; set; }
        public int? StatusCode { get; set; }
        public string? ResponseBody { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? ExpiresOn { get; set; }
        public DateTime? LockedUntil { get; set; }
    }
}
