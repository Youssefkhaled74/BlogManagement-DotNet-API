using System.Text.Json;
using BlogManagement.Application.Security;
using BlogManagement.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BlogManagement.Infrastructure.Persistence;

public static class DemoDataSeeder
{
    public const string DefaultPassword = "DemoPass123!";
    private static Guid Id(int number) => Guid.Parse($"de000000-0000-0000-0000-{number:000000000000}");

    public static async Task SeedAsync(BlogManagementDbContext db, string password = DefaultPassword, CancellationToken ct = default)
    {
        if (password.Length is < 8 or > 100) throw new InvalidOperationException("Demo password must contain 8–100 characters.");
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var roles = await db.Roles.Include(x => x.Permissions).ToDictionaryAsync(x => x.Name, ct);
        var support = await db.Roles.FirstOrDefaultAsync(x => x.Id == Id(10), ct);
        if (support is null)
        {
            support = new Role { Id = Id(10), Name = "Demo Support", Description = "Read-only demo role for support staff" };
            var names = new List<string> { Permissions.UsersRead, Permissions.EmployeesRead, Permissions.AuditRead, Permissions.BlogsRead, Permissions.CategoriesRead };
            support.Permissions = await db.Permissions.Where(x => names.Contains(x.Name)).ToListAsync(ct);
            db.Roles.Add(support);
        }
        roles["Demo Support"] = support;
        var people = new[]
        {
            ("admin", "Demo Administrator", SystemRoles.Admin, "Administration", "Workspace Manager"),
            ("editor", "Nour Hassan", SystemRoles.Editor, "Editorial", "Senior Editor"),
            ("sara", "Sara Ahmed", SystemRoles.Author, "Content", "Content Writer"),
            ("omar", "Omar Ali", SystemRoles.Author, "Engineering", "Technical Writer"),
            ("support", "Mona Samir", "Demo Support", "Support", "Support Specialist"),
            ("inactive", "Karim Adel", SystemRoles.Author, "Content", "Former Contributor")
        };
        var epoch = new DateTime(2026, 9, 1, 9, 0, 0, DateTimeKind.Utc);
        for (var i = 0; i < people.Length; i++)
        {
            var person = people[i];
            var userId = Id(100 + i);
            if (!await db.Users.AnyAsync(x => x.Id == userId, ct))
            {
                var user = new User { Id = userId, Username = $"demo.{person.Item1}", Email = $"{person.Item1}.demo@example.test", DisplayName = person.Item2,
                    IsActive = i != 5, CreatedAt = epoch.AddDays(i), ProfileImageUrl = $"/demo-images/avatar-{i % 4}.svg", Roles = [roles[person.Item3]] };
                user.PasswordHash = new PasswordHasher<User>().HashPassword(user, password);
                db.Users.Add(user);
            }
            if (!await db.Employees.AnyAsync(x => x.Id == Id(200 + i), ct))
                db.Employees.Add(new Employee { Id = Id(200 + i), FirstName = person.Item2.Split(' ')[0], LastName = string.Join(' ', person.Item2.Split(' ').Skip(1)),
                    EmployeeNumber = $"DEMO-{i + 1:000}", Department = person.Item4, JobTitle = person.Item5, UserId = userId, IsActive = i != 5, CreatedAt = epoch.AddDays(i) });
        }
        await db.SaveChangesAsync(ct);
        var categories = await db.Categories.OrderBy(x => x.Name).Take(6).ToListAsync(ct);
        if (categories.Count == 0) throw new InvalidOperationException("Seed default categories before demo data.");
        var titles = new[] { "Building a better writing routine", "Getting started with React components", "How AI helps editorial teams", "A practical guide to clean APIs", "Learning something new every week", "Making room for thoughtful work", "The craft of a useful code review", "Planning your next side project", "Small habits that improve productivity", "Turning ideas into published stories", "Understanding roles and permissions", "Writing documentation people can use" };
        var auditIds = (await db.AuditLogs.Select(x => x.Id).ToListAsync(ct)).ToHashSet();
        void AddAudit(AuditLog entry)
        {
            if (auditIds.Add(entry.Id)) db.AuditLogs.Add(entry);
        }
        for (var i = 0; i < 36; i++)
        {
            var blogId = Id(1000 + i);
            if (await db.Blogs.AnyAsync(x => x.Id == blogId, ct)) continue;
            var status = i < 16 ? BlogStatus.Published : i < 22 ? BlogStatus.Draft : i < 27 ? BlogStatus.PendingApproval : i < 32 ? BlogStatus.Approved : BlogStatus.Rejected;
            var category = categories[i % categories.Count];
            var title = titles[i % titles.Length] + (i >= titles.Length ? $" · Part {i / titles.Length + 1}" : "");
            var at = epoch.AddDays(i % 28).AddHours(i % 5);
            var authorId = Id(i % 2 == 0 ? 102 : 103);
            var reviewed = status is BlogStatus.Approved or BlogStatus.Published or BlogStatus.Rejected;
            var comment = status == BlogStatus.Rejected ? "Please add a concrete example and verify your sources before resubmitting." : reviewed ? "Clear structure and useful examples. Ready for publication." : null;
            db.Blogs.Add(new Blog { Id = blogId, Title = title, Slug = $"demo-story-{i + 1:00}", Content = $"{title}\n\nGood stories start with a clear question. This sample article explores practical ideas in {category.Name.ToLowerInvariant()} and gives our readers something they can apply today.\n\nStart with one small experiment\n\nChoose a problem you understand, write down your assumptions, and try a manageable first step. Keep notes about what worked and what needs improvement. Ask a colleague to review your approach and use their feedback to make the next version more useful.\n\nWhat to take away\n\nConsistency matters more than a perfect first attempt. Keep your examples concrete, explain your decisions, and make time to reflect on the result.\n\nThis is editable demonstration content for testing the editorial workflow.",
                Status = status, AuthorId = authorId, CategoryId = category.Id, CreatedAt = at, UpdatedAt = status == BlogStatus.Draft ? null : at.AddHours(2), PublishedAt = status == BlogStatus.Published ? at.AddHours(4) : null,
                ReviewComment = comment, ImageUrl = $"/demo-images/cover-{i % 6}.svg" });
            if (reviewed)
                db.BlogApprovalHistories.Add(new BlogApprovalHistory { Id = Id(2000 + i), BlogId = blogId, ApprovedByUserId = Id(101), Decision = status == BlogStatus.Rejected ? BlogStatus.Rejected : BlogStatus.Approved, Comment = comment, Timestamp = at.AddHours(3) });
            AddAudit(new AuditLog { Id = Id(3000 + i), Action = AuditAction.Create, EntityName = nameof(Blog), EntityId = blogId, PerformedByUserId = authorId, Timestamp = at,
                Data = JsonSerializer.Serialize(new { Title = title, Demo = true }) });
            if (status != BlogStatus.Draft)
                AddAudit(new AuditLog { Id = Id(4000 + i), Action = AuditAction.Submit, EntityName = nameof(Blog), EntityId = blogId, PerformedByUserId = authorId, Timestamp = at.AddHours(1), Data = "{\"demo\":true}" });
            if (reviewed)
                AddAudit(new AuditLog { Id = Id(5000 + i), Action = status == BlogStatus.Rejected ? AuditAction.Reject : AuditAction.Approve, EntityName = nameof(Blog), EntityId = blogId, PerformedByUserId = Id(101), Timestamp = at.AddHours(3), Data = JsonSerializer.Serialize(new { Comment = comment, Demo = true }) });
            if (status == BlogStatus.Published)
                AddAudit(new AuditLog { Id = Id(6000 + i), Action = AuditAction.Publish, EntityName = nameof(Blog), EntityId = blogId, PerformedByUserId = Id(101), Timestamp = at.AddHours(4), Data = "{\"demo\":true}" });
        }
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }
}
