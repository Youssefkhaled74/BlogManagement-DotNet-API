using System;

namespace BlogManagement.Domain.Entities
{
    public class RefreshToken
    {
        public Guid Id { get; set; }
        // We store a SHA256 hash of the token for safety
        public string TokenHash { get; set; } = null!;
        public Guid UserId { get; set; }
        public User? User { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? RevokedAt { get; set; }
        public string? ReplacedByTokenHash { get; set; }

        public bool IsActive => RevokedAt == null && !IsExpired;
        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    }
}
