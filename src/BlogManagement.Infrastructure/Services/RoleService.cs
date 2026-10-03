using BlogManagement.Application.DTOs;
using BlogManagement.Application.Interfaces;
using BlogManagement.Application.Security;
using BlogManagement.Domain.Entities;
using BlogManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BlogManagement.Infrastructure.Services;

public sealed class RoleService : IRoleService
{
    private readonly BlogManagementDbContext _db;
    private readonly IAuditService _audit;
    public RoleService(BlogManagementDbContext db, IAuditService audit) => (_db, _audit) = (db, audit);

    public async Task<IReadOnlyCollection<RoleDto>> GetAllAsync(CancellationToken ct = default) =>
        await _db.Roles.AsNoTracking().OrderBy(x => x.Name).Select(x => new RoleDto(
            x.Id, x.Name, x.Description, x.Permissions.OrderBy(p => p.Name).Select(p => p.Name).ToList())).ToListAsync(ct);

    public async Task<IReadOnlyCollection<PermissionDto>> GetPermissionsAsync(CancellationToken ct = default) =>
        await _db.Permissions.AsNoTracking().OrderBy(x => x.Name)
            .Select(x => new PermissionDto(x.Id, x.Name, x.Description)).ToListAsync(ct);

    public async Task<RoleDto> GetAsync(Guid id, CancellationToken ct = default) =>
        await Map(_db.Roles.AsNoTracking().Where(x => x.Id == id)).FirstOrDefaultAsync(ct)
        ?? throw new KeyNotFoundException("Role not found.");

    public async Task<RoleDto> CreateAsync(CreateRoleDto request, Guid actorId, CancellationToken ct = default)
    {
        var name = request.Name.Trim();
        if (await _db.Roles.AnyAsync(x => x.Name == name, ct)) throw new InvalidOperationException("Role name already exists.");
        var role = new Role { Id = Guid.NewGuid(), Name = name, Description = request.Description?.Trim() };
        if (request.Permissions is not null)
            role.Permissions = await ResolvePermissionsAsync(request.Permissions, ct);
        _db.Roles.Add(role);
        await _db.SaveChangesAsync(ct);
        await _audit.WriteAsync(AuditAction.Create, nameof(Role), role.Id, actorId, new { role.Name }, ct);
        return await GetAsync(role.Id, ct);
    }

    public async Task<RoleDto> UpdateAsync(Guid id, UpdateRoleDto request, Guid actorId, CancellationToken ct = default)
    {
        var role = await _db.Roles.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new KeyNotFoundException("Role not found.");
        var name = request.Name.Trim();
        if (await _db.Roles.AnyAsync(x => x.Id != id && x.Name == name, ct)) throw new InvalidOperationException("Role name already exists.");
        if (IsSystemRole(role.Name) && role.Name != name) throw new InvalidOperationException("System roles cannot be renamed.");
        role.Name = name;
        role.Description = request.Description?.Trim();
        await _db.SaveChangesAsync(ct);
        await _audit.WriteAsync(AuditAction.Update, nameof(Role), id, actorId, new { role.Name }, ct);
        return await GetAsync(id, ct);
    }

    public async Task<RoleDto> SetPermissionsAsync(Guid id, IReadOnlyCollection<string> permissions, Guid actorId, CancellationToken ct = default)
    {
        var role = await _db.Roles.Include(x => x.Permissions).FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("Role not found.");
        if (role.Name == SystemRoles.Admin && Permissions.All.Except(permissions, StringComparer.OrdinalIgnoreCase).Any())
            throw new InvalidOperationException("The Admin role must keep every system permission.");
        role.Permissions.Clear();
        foreach (var permission in await ResolvePermissionsAsync(permissions, ct)) role.Permissions.Add(permission);
        await _db.SaveChangesAsync(ct);
        await _audit.WriteAsync(AuditAction.Assign, nameof(Role), id, actorId, new { Permissions = permissions }, ct);
        return await GetAsync(id, ct);
    }

    public async Task DeleteAsync(Guid id, Guid actorId, CancellationToken ct = default)
    {
        var role = await _db.Roles.Include(x => x.Users).FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("Role not found.");
        if (IsSystemRole(role.Name)) throw new InvalidOperationException("System roles cannot be deleted.");
        if (role.Users.Count != 0) throw new InvalidOperationException("Cannot delete a role assigned to users.");
        _db.Roles.Remove(role);
        await _db.SaveChangesAsync(ct);
        await _audit.WriteAsync(AuditAction.Delete, nameof(Role), id, actorId, new { role.Name }, ct);
    }

    private async Task<ICollection<Permission>> ResolvePermissionsAsync(IEnumerable<string> names, CancellationToken ct)
    {
        var requested = names.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var permissions = await _db.Permissions.Where(x => requested.Contains(x.Name)).ToListAsync(ct);
        var missing = requested.Except(permissions.Select(x => x.Name), StringComparer.OrdinalIgnoreCase).ToArray();
        if (missing.Length != 0) throw new InvalidOperationException($"Unknown permissions: {string.Join(", ", missing)}");
        return permissions;
    }

    private static IQueryable<RoleDto> Map(IQueryable<Role> query) => query.Select(x => new RoleDto(
        x.Id, x.Name, x.Description, x.Permissions.OrderBy(p => p.Name).Select(p => p.Name).ToList()));

    private static bool IsSystemRole(string name) => name is SystemRoles.Admin or SystemRoles.Editor or SystemRoles.Author;
}
