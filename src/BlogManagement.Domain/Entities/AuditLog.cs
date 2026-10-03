using System;

namespace BlogManagement.Domain.Entities
{
    public enum AuditAction
    {
        Create,
        Update,
        Delete,
        Publish,
        Unpublish,
        Approve,
        Reject,
        Submit,
        Activate,
        Deactivate,
        Assign
    }

    public class AuditLog
    {
        public Guid Id { get; set; }
        public AuditAction Action { get; set; }
        public string EntityName { get; set; } = null!;
        public Guid EntityId { get; set; }
        public Guid PerformedByUserId { get; set; }
        public User? PerformedBy { get; set; }
        public DateTime Timestamp { get; set; }
        public string? Data { get; set; }
    }
}
