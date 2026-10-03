using System;
namespace BlogManagement.Domain.Entities
{
    public class Employee
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string? Title { get; set; }
        public Guid? UserId { get; set; }
        public User? User { get; set; }
        public string? EmployeeNumber { get; set; }
        public string? Department { get; set; }
        public string? JobTitle { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
