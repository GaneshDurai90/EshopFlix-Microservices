using System;

namespace AuthService.Domain.Entities
{
    public class RefreshToken
    {
        public int Id { get; set; }
        public string Token { get; set; } = string.Empty;
        public int UserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime? RevokedAt { get; set; }
        public string? ReplacedByToken { get; set; }

        public virtual User? User { get; set; }

        public bool IsActive => RevokedAt == null && DateTime.UtcNow <= ExpiresAt;
    }
}
