using System;
namespace BlogManagement.Domain.Entities
{
    public class Permission
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public ICollection<Role> Roles { get; set; } = new List<Role>();
    }
}
