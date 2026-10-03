using System.Text.RegularExpressions;
using BlogManagement.Application.DTOs;
using BlogManagement.Application.Interfaces;
using BlogManagement.Domain.Entities;
using BlogManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BlogManagement.Infrastructure.Services;

public sealed class BlogService : IBlogService
{
    private readonly BlogManagementDbContext _db;
    private readonly IAuditService _audit;
    public BlogService(BlogManagementDbContext db, IAuditService audit) => (_db, _audit) = (db, audit);

    public async Task<PagedResult<BlogDto>> GetAllAsync(Guid userId, bool canReviewAll, int page, int pageSize, BlogStatus? status, string? search, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = _db.Blogs.AsNoTracking().AsQueryable();
        if (!canReviewAll) query = query.Where(x => x.AuthorId == userId || x.Status == BlogStatus.Published);
        if (status.HasValue) query = query.Where(x => x.Status == status);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.Title.Contains(term) || x.Content.Contains(term));
        }

        var total = await query.CountAsync(ct);
        var items = await Project(query.OrderByDescending(x => x.CreatedAt))
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new PagedResult<BlogDto>(items, page, pageSize, total);
    }

    public async Task<BlogDto> GetAsync(Guid id, Guid userId, bool canReviewAll, CancellationToken ct = default)
    {
        var query = _db.Blogs.AsNoTracking().Where(x => x.Id == id);
        if (!canReviewAll) query = query.Where(x => x.AuthorId == userId || x.Status == BlogStatus.Published);
        return await Project(query).FirstOrDefaultAsync(ct) ?? throw new KeyNotFoundException("Blog not found.");
    }

    public async Task<BlogDto> CreateAsync(CreateBlogDto request, Guid actorId, CancellationToken ct = default)
    {
        await EnsureCategoryAsync(request.CategoryId, ct);
        var slug = await UniqueSlugAsync(request.Slug ?? request.Title, null, ct);
        var blog = new Blog
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Content = request.Content.Trim(),
            Slug = slug,
            CategoryId = request.CategoryId,
            AuthorId = actorId,
            Status = BlogStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };
        _db.Blogs.Add(blog);
        await _db.SaveChangesAsync(ct);
        await _audit.WriteAsync(AuditAction.Create, nameof(Blog), blog.Id, actorId, new { blog.Title, blog.Status }, ct);
        return await LoadDtoAsync(blog.Id, ct);
    }

    public async Task<BlogDto> UpdateAsync(Guid id, UpdateBlogDto request, Guid actorId, bool canManageAll, CancellationToken ct = default)
    {
        var blog = await _db.Blogs.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("Blog not found.");
        EnsureOwnerOrManager(blog, actorId, canManageAll);
        if (blog.Status is BlogStatus.PendingApproval or BlogStatus.Published)
            throw new InvalidOperationException("A pending or published blog cannot be edited.");

        await EnsureCategoryAsync(request.CategoryId, ct);
        blog.Title = request.Title.Trim();
        blog.Content = request.Content.Trim();
        blog.Slug = await UniqueSlugAsync(request.Slug ?? request.Title, id, ct);
        blog.CategoryId = request.CategoryId;
        blog.UpdatedAt = DateTime.UtcNow;
        blog.Status = BlogStatus.Draft;
        blog.ReviewComment = null;
        await _db.SaveChangesAsync(ct);
        await _audit.WriteAsync(AuditAction.Update, nameof(Blog), id, actorId, new { blog.Title }, ct);
        return await LoadDtoAsync(id, ct);
    }

    public async Task DeleteAsync(Guid id, Guid actorId, bool canManageAll, CancellationToken ct = default)
    {
        var blog = await _db.Blogs.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("Blog not found.");
        EnsureOwnerOrManager(blog, actorId, canManageAll);
        if (blog.Status is BlogStatus.PendingApproval or BlogStatus.Published)
            throw new InvalidOperationException("A pending or published blog cannot be deleted.");

        _db.Blogs.Remove(blog);
        await _db.SaveChangesAsync(ct);
        await _audit.WriteAsync(AuditAction.Delete, nameof(Blog), id, actorId, new { blog.Title }, ct);
    }

    public async Task<BlogDto> SubmitAsync(Guid id, Guid actorId, CancellationToken ct = default)
    {
        var blog = await _db.Blogs.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("Blog not found.");
        if (blog.AuthorId != actorId) throw new ForbiddenException("Only the author can submit this blog.");
        if (blog.Status is not (BlogStatus.Draft or BlogStatus.Rejected))
            throw new InvalidOperationException("Only draft or rejected blogs can be submitted.");

        blog.Status = BlogStatus.PendingApproval;
        blog.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        await _audit.WriteAsync(AuditAction.Submit, nameof(Blog), id, actorId, new { blog.Status }, ct);
        return await LoadDtoAsync(id, ct);
    }

    public async Task<BlogDto> ReviewAsync(Guid id, ReviewBlogDto request, Guid actorId, CancellationToken ct = default)
    {
        var blog = await _db.Blogs.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("Blog not found.");
        if (blog.Status != BlogStatus.PendingApproval)
            throw new InvalidOperationException("Only pending blogs can be reviewed.");

        blog.Status = request.Approved ? BlogStatus.Approved : BlogStatus.Rejected;
        blog.ReviewComment = request.Comment?.Trim();
        blog.UpdatedAt = DateTime.UtcNow;
        _db.BlogApprovalHistories.Add(new BlogApprovalHistory
        {
            Id = Guid.NewGuid(),
            BlogId = id,
            ApprovedByUserId = actorId,
            Decision = blog.Status,
            Comment = blog.ReviewComment,
            Timestamp = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);
        await _audit.WriteAsync(request.Approved ? AuditAction.Approve : AuditAction.Reject,
            nameof(Blog), id, actorId, new { blog.Status, blog.ReviewComment }, ct);
        return await LoadDtoAsync(id, ct);
    }

    public async Task<BlogDto> PublishAsync(Guid id, Guid actorId, CancellationToken ct = default)
    {
        var blog = await _db.Blogs.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("Blog not found.");
        if (blog.Status != BlogStatus.Approved)
            throw new InvalidOperationException("Only approved blogs can be published.");

        blog.Status = BlogStatus.Published;
        blog.PublishedAt = DateTime.UtcNow;
        blog.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        await _audit.WriteAsync(AuditAction.Publish, nameof(Blog), id, actorId, new { blog.PublishedAt }, ct);
        return await LoadDtoAsync(id, ct);
    }

    public async Task<BlogDto> UnpublishAsync(Guid id, Guid actorId, CancellationToken ct = default)
    {
        var blog = await _db.Blogs.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("Blog not found.");
        if (blog.Status != BlogStatus.Published)
            throw new InvalidOperationException("Only published blogs can be unpublished.");

        blog.Status = BlogStatus.Approved;
        blog.PublishedAt = null;
        blog.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        await _audit.WriteAsync(AuditAction.Unpublish, nameof(Blog), id, actorId, null, ct);
        return await LoadDtoAsync(id, ct);
    }

    public async Task<IReadOnlyCollection<BlogApprovalHistoryDto>> GetHistoryAsync(Guid id, CancellationToken ct = default)
    {
        if (!await _db.Blogs.AnyAsync(x => x.Id == id, ct)) throw new KeyNotFoundException("Blog not found.");
        return await _db.BlogApprovalHistories.AsNoTracking().Where(x => x.BlogId == id)
            .OrderByDescending(x => x.Timestamp)
            .Select(x => new BlogApprovalHistoryDto(x.Id, x.Decision, x.ApprovedByUserId,
                x.ApprovedBy == null ? "Unknown" : x.ApprovedBy.Username, x.Comment, x.Timestamp))
            .ToListAsync(ct);
    }

    private async Task<BlogDto> LoadDtoAsync(Guid id, CancellationToken ct) =>
        await Project(_db.Blogs.AsNoTracking().Where(x => x.Id == id)).FirstAsync(ct);

    private static IQueryable<BlogDto> Project(IQueryable<Blog> query) => query.Select(x => new BlogDto(
        x.Id, x.Title, x.Slug, x.Content, x.Status, x.AuthorId,
        x.Author == null ? "Unknown" : x.Author.Username,
        x.CategoryId, x.Category == null ? "Unknown" : x.Category.Name,
        x.CreatedAt, x.UpdatedAt, x.PublishedAt, x.ReviewComment));

    private async Task EnsureCategoryAsync(Guid categoryId, CancellationToken ct)
    {
        if (!await _db.Categories.AnyAsync(x => x.Id == categoryId, ct))
            throw new InvalidOperationException("Selected category does not exist.");
    }

    private async Task<string> UniqueSlugAsync(string value, Guid? currentId, CancellationToken ct)
    {
        var baseSlug = Regex.Replace(value.Trim().ToLowerInvariant(), @"[^\p{L}\p{N}]+", "-").Trim('-');
        if (string.IsNullOrWhiteSpace(baseSlug)) baseSlug = $"blog-{Guid.NewGuid():N}";
        var slug = baseSlug;
        var suffix = 2;
        while (await _db.Blogs.AnyAsync(x => (!currentId.HasValue || x.Id != currentId.Value) && x.Slug == slug, ct))
            slug = $"{baseSlug}-{suffix++}";
        return slug;
    }

    private static void EnsureOwnerOrManager(Blog blog, Guid actorId, bool canManageAll)
    {
        if (blog.AuthorId != actorId && !canManageAll)
            throw new ForbiddenException("You can only manage your own blogs.");
    }
}

public sealed class ForbiddenException(string message) : Exception(message) { }
