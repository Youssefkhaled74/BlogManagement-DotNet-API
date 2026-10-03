using System;
using System.Collections.Generic;

namespace BlogManagement.Domain.Entities
{
    public class Role
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }

        public ICollection<Permission> Permissions { get; set; } = new List<Permission>();
        public ICollection<User> Users { get; set; } = new List<User>();
    }
}
