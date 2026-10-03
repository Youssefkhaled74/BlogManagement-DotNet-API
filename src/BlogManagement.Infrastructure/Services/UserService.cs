using BlogManagement.Application.DTOs;
using BlogManagement.Application.Interfaces;
using BlogManagement.Domain.Entities;
using BlogManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BlogManagement.Infrastructure.Services;

public sealed class UserService : IUserService
{
    private readonly BlogManagementDbContext _db;
    private readonly IAuditService _audit;
    public UserService(BlogManagementDbContext db, IAuditService audit) => (_db, _audit) = (db, audit);

    public async Task<PagedResult<UserSummaryDto>> GetAllAsync(int page, int pageSize, string? search, CancellationToken ct = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = _db.Users.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.Username.Contains(term) || x.Email.Contains(term));
        }
        var total = await query.CountAsync(ct);
        var items = await Map(query.OrderBy(x => x.Username)).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new PagedResult<UserSummaryDto>(items, page, pageSize, total);
    }

    public async Task<UserSummaryDto> GetAsync(Guid id, CancellationToken ct = default) =>
        await Map(_db.Users.AsNoTracking().Where(x => x.Id == id)).FirstOrDefaultAsync(ct)
        ?? throw new KeyNotFoundException("User not found.");

    public async Task<UserSummaryDto> UpdateAsync(Guid id, UpdateUserDto request, Guid actorId, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new KeyNotFoundException("User not found.");
        var email = request.Email.Trim().ToLowerInvariant();
        var username = request.Username.Trim();
        if (await _db.Users.AnyAsync(x => x.Id != id && x.Email == email, ct)) throw new InvalidOperationException("Email already in use.");
        if (await _db.Users.AnyAsync(x => x.Id != id && x.Username == username, ct)) throw new InvalidOperationException("Username already in use.");
        user.Email = email;
        user.Username = username;
        user.DisplayName = request.DisplayName?.Trim();
        await _db.SaveChangesAsync(ct);
        await _audit.WriteAsync(AuditAction.Update, nameof(User), id, actorId, new { user.Username, user.Email }, ct);
        return await GetAsync(id, ct);
    }

    public async Task<UserSummaryDto> SetStatusAsync(Guid id, bool isActive, Guid actorId, CancellationToken ct = default)
    {
        if (id == actorId && !isActive) throw new InvalidOperationException("You cannot deactivate your own account.");
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new KeyNotFoundException("User not found.");
        user.IsActive = isActive;
        if (!isActive)
        {
            await _db.RefreshTokens.Where(x => x.UserId == id && x.RevokedAt == null)
                .ExecuteUpdateAsync(x => x.SetProperty(t => t.RevokedAt, DateTime.UtcNow), ct);
        }
        await _db.SaveChangesAsync(ct);
        await _audit.WriteAsync(isActive ? AuditAction.Activate : AuditAction.Deactivate, nameof(User), id, actorId, null, ct);
        return await GetAsync(id, ct);
    }

    public async Task<UserSummaryDto> SetRolesAsync(Guid id, IReadOnlyCollection<Guid> roleIds, Guid actorId, CancellationToken ct = default)
    {
        if (id == actorId) throw new InvalidOperationException("You cannot change your own roles.");
        var user = await _db.Users.Include(x => x.Roles).FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("User not found.");
        var requested = roleIds.Distinct().ToArray();
        var roles = await _db.Roles.Where(x => requested.Contains(x.Id)).ToListAsync(ct);
        if (roles.Count != requested.Length) throw new InvalidOperationException("One or more roles do not exist.");
        user.Roles.Clear();
        foreach (var role in roles) user.Roles.Add(role);
        await _db.SaveChangesAsync(ct);
        await _audit.WriteAsync(AuditAction.Assign, nameof(User), id, actorId, new { RoleIds = requested }, ct);
        return await GetAsync(id, ct);
    }

    private static IQueryable<UserSummaryDto> Map(IQueryable<User> query) => query.Select(x => new UserSummaryDto(
        x.Id, x.Username, x.Email, x.DisplayName, x.IsActive, x.CreatedAt, x.LastLoginAt,
        x.Roles.OrderBy(r => r.Name).Select(r => r.Name).ToList()));
}
