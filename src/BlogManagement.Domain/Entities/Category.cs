using System;
using System.Collections.Generic;

namespace BlogManagement.Domain.Entities
{
    public class Category
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }

        public ICollection<Blog> Blogs { get; set; } = new List<Blog>();
    }
}
