using BlogManagement.Application.Security;
using BlogManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BlogManagement.Infrastructure.Persistence;

public static class DataSeeder
{
    private static readonly IReadOnlyDictionary<string, string> PermissionDescriptions = new Dictionary<string, string>
    {
        [Permissions.UsersRead] = "View users",
        [Permissions.UsersManage] = "Update users, status and role assignments",
        [Permissions.RolesRead] = "View roles and permissions",
        [Permissions.RolesManage] = "Create and update roles",
        [Permissions.CategoriesRead] = "View categories",
        [Permissions.CategoriesManage] = "Create, update and delete categories",
        [Permissions.BlogsRead] = "View blogs",
        [Permissions.BlogsCreate] = "Create blogs",
        [Permissions.BlogsUpdate] = "Update blogs",
        [Permissions.BlogsDelete] = "Delete blogs",
        [Permissions.BlogsSubmit] = "Submit blogs for review",
        [Permissions.BlogsReview] = "Approve or reject blogs",
        [Permissions.BlogsPublish] = "Publish and unpublish approved blogs",
        [Permissions.EmployeesRead] = "View employees",
        [Permissions.EmployeesManage] = "Create, update and delete employees",
        [Permissions.AuditRead] = "View audit logs"
    };

    public static async Task SeedAsync(BlogManagementDbContext db, CancellationToken ct = default)
    {
        var existingPermissions = await db.Permissions.ToListAsync(ct);
        foreach (var name in Permissions.All)
        {
            if (existingPermissions.All(x => x.Name != name))
            {
                db.Permissions.Add(new Permission
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    Description = PermissionDescriptions[name]
                });
            }
        }
        await db.SaveChangesAsync(ct);

        var permissions = await db.Permissions.ToDictionaryAsync(x => x.Name, ct);
        await EnsureRoleAsync(db, SystemRoles.Admin, "Full system access", Permissions.All, permissions, ct);
        await EnsureRoleAsync(db, SystemRoles.Editor, "Reviews and publishes content",
        [
            Permissions.CategoriesRead, Permissions.CategoriesManage,
            Permissions.BlogsRead, Permissions.BlogsUpdate, Permissions.BlogsReview, Permissions.BlogsPublish,
            Permissions.AuditRead
        ], permissions, ct);
        await EnsureRoleAsync(db, SystemRoles.Author, "Creates and submits own content",
        [
            Permissions.CategoriesRead, Permissions.BlogsRead, Permissions.BlogsCreate,
            Permissions.BlogsUpdate, Permissions.BlogsDelete, Permissions.BlogsSubmit
        ], permissions, ct);
        await db.SaveChangesAsync(ct);
    }

    private static async Task EnsureRoleAsync(
        BlogManagementDbContext db,
        string name,
        string description,
        IEnumerable<string> permissionNames,
        IReadOnlyDictionary<string, Permission> permissions,
        CancellationToken ct)
    {
        var role = await db.Roles.Include(x => x.Permissions).FirstOrDefaultAsync(x => x.Name == name, ct);
        if (role is null)
        {
            role = new Role { Id = Guid.NewGuid(), Name = name, Description = description };
            db.Roles.Add(role);
        }

        foreach (var permissionName in permissionNames)
        {
            var permission = permissions[permissionName];
            if (role.Permissions.All(x => x.Id != permission.Id))
                role.Permissions.Add(permission);
        }
    }
}
