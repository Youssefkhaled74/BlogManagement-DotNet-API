using System;
using System.Collections.Generic;

namespace BlogManagement.Domain.Entities
{
    public enum BlogStatus
    {
        Draft = 0,
        PendingApproval = 1,
        Approved = 2,
        Published = 3,
        Rejected = 4
    }

    public class Blog
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string Content { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? PublishedAt { get; set; }
        public string? ReviewComment { get; set; }
        public BlogStatus Status { get; set; } = BlogStatus.Draft;

        public Guid AuthorId { get; set; }
        public User? Author { get; set; }

        public Guid CategoryId { get; set; }
        public Category? Category { get; set; }

        public ICollection<BlogApprovalHistory> ApprovalHistory { get; set; } = new List<BlogApprovalHistory>();
    }
}
