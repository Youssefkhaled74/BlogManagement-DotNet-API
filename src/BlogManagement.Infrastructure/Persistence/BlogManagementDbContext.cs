using System;
using Microsoft.EntityFrameworkCore;
using BlogManagement.Domain.Entities;

namespace BlogManagement.Infrastructure.Persistence
{
    public class BlogManagementDbContext : DbContext
    {
        public BlogManagementDbContext(DbContextOptions<BlogManagementDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<Permission> Permissions => Set<Permission>();
        public DbSet<Blog> Blogs => Set<Blog>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<BlogApprovalHistory> BlogApprovalHistories => Set<BlogApprovalHistory>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<Employee> Employees => Set<Employee>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User
            modelBuilder.Entity<User>(b =>
            {
                b.HasKey(x => x.Id);
                b.HasIndex(x => x.Email).IsUnique();
                b.HasIndex(x => x.Username).IsUnique();
                b.Property(x => x.Email).IsRequired();
                b.Property(x => x.Username).IsRequired();
                b.HasMany(x => x.Roles).WithMany(r => r.Users);
            });

            // Role - Permission many-to-many
            modelBuilder.Entity<Role>(b =>
            {
                b.HasKey(x => x.Id);
                b.HasIndex(x => x.Name).IsUnique();
                b.Property(x => x.Name).IsRequired();
                b.HasMany(x => x.Permissions).WithMany();
            });

            modelBuilder.Entity<Permission>(b =>
            {
                b.HasKey(x => x.Id);
                b.HasIndex(x => x.Name).IsUnique();
                b.Property(x => x.Name).IsRequired();
            });

            modelBuilder.Entity<Category>(b => { b.HasKey(x => x.Id); b.HasIndex(x => x.Name); });

            modelBuilder.Entity<Blog>(b =>
            {
                b.HasKey(x => x.Id);
                b.HasIndex(x => x.Slug).IsUnique();
                b.Property(x => x.Title).IsRequired();
                b.Property(x => x.Content).IsRequired();
                b.HasOne(x => x.Author).WithMany().HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.Restrict);
                b.HasOne(x => x.Category).WithMany(c => c.Blogs).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<BlogApprovalHistory>(b => { b.HasKey(x => x.Id); });
            modelBuilder.Entity<AuditLog>(b => { b.HasKey(x => x.Id); });

            modelBuilder.Entity<Employee>(b =>
            {
                b.HasKey(x => x.Id);
                b.HasIndex(x => x.EmployeeNumber).IsUnique(false);
                b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<RefreshToken>(b =>
            {
                b.HasKey(x => x.Id);
                b.HasIndex(x => x.TokenHash);
                b.HasOne(x => x.User).WithMany(u => u.RefreshTokens).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
