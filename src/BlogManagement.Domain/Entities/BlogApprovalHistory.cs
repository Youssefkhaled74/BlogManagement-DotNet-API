using System;

namespace BlogManagement.Domain.Entities
{
    public class BlogApprovalHistory
    {
        public Guid Id { get; set; }
        public Guid BlogId { get; set; }
        public Blog? Blog { get; set; }
        public Guid ApprovedByUserId { get; set; }
        public User? ApprovedBy { get; set; }
        public BlogStatus Decision { get; set; }
        public string? Comment { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
