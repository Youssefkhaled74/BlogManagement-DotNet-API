using System.Text.Json;
using BlogManagement.Application.DTOs;
using BlogManagement.Application.Interfaces;
using BlogManagement.Domain.Entities;
using BlogManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BlogManagement.Infrastructure.Services;

public sealed class AuditService : IAuditService
{
    private readonly BlogManagementDbContext _db;
    public AuditService(BlogManagementDbContext db) => _db = db;

    public async Task WriteAsync(AuditAction action, string entityName, Guid entityId, Guid actorId, object? data = null, CancellationToken ct = default)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            PerformedByUserId = actorId,
            Timestamp = DateTime.UtcNow,
            Data = data is null ? null : JsonSerializer.Serialize(data)
        });
        await _db.SaveChangesAsync(ct);
    }

    public async Task<PagedResult<AuditLogDto>> GetAllAsync(int page, int pageSize, string? entityName, AuditAction? action, CancellationToken ct = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = _db.AuditLogs.AsNoTracking().Include(x => x.PerformedBy).AsQueryable();
        if (!string.IsNullOrWhiteSpace(entityName)) query = query.Where(x => x.EntityName == entityName);
        if (action.HasValue) query = query.Where(x => x.Action == action);

        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.Timestamp)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new AuditLogDto(x.Id, x.Action, x.EntityName, x.EntityId, x.PerformedByUserId,
                x.PerformedBy == null ? null : x.PerformedBy.Username, x.Timestamp, x.Data))
            .ToListAsync(ct);
        return new PagedResult<AuditLogDto>(items, page, pageSize, total);
    }
}
