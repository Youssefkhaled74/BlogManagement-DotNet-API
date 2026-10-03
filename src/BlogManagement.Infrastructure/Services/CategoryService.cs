using BlogManagement.Application.DTOs;
using BlogManagement.Application.Interfaces;
using BlogManagement.Domain.Entities;
using BlogManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BlogManagement.Infrastructure.Services;

public sealed class CategoryService : ICategoryService
{
    private readonly BlogManagementDbContext _db;
    private readonly IAuditService _audit;
    public CategoryService(BlogManagementDbContext db, IAuditService audit) => (_db, _audit) = (db, audit);

    public async Task<IReadOnlyCollection<CategoryDto>> GetAllAsync(CancellationToken ct = default) =>
        await _db.Categories.AsNoTracking().OrderBy(x => x.Name)
            .Select(x => new CategoryDto(x.Id, x.Name, x.Description, x.Blogs.Count))
            .ToListAsync(ct);

    public async Task<CategoryDto> GetAsync(Guid id, CancellationToken ct = default) =>
        await _db.Categories.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new CategoryDto(x.Id, x.Name, x.Description, x.Blogs.Count))
            .FirstOrDefaultAsync(ct) ?? throw new KeyNotFoundException("Category not found.");

    public async Task<CategoryDto> CreateAsync(CreateCategoryDto request, Guid actorId, CancellationToken ct = default)
    {
        var name = request.Name.Trim();
        if (await _db.Categories.AnyAsync(x => x.Name == name, ct))
            throw new InvalidOperationException("Category name already exists.");

        var category = new Category { Id = Guid.NewGuid(), Name = name, Description = request.Description?.Trim() };
        _db.Categories.Add(category);
        await _db.SaveChangesAsync(ct);
        await _audit.WriteAsync(AuditAction.Create, nameof(Category), category.Id, actorId, new { category.Name }, ct);
        return new CategoryDto(category.Id, category.Name, category.Description, 0);
    }

    public async Task<CategoryDto> UpdateAsync(Guid id, UpdateCategoryDto request, Guid actorId, CancellationToken ct = default)
    {
        var category = await _db.Categories.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("Category not found.");
        var name = request.Name.Trim();
        if (await _db.Categories.AnyAsync(x => x.Id != id && x.Name == name, ct))
            throw new InvalidOperationException("Category name already exists.");

        category.Name = name;
        category.Description = request.Description?.Trim();
        await _db.SaveChangesAsync(ct);
        await _audit.WriteAsync(AuditAction.Update, nameof(Category), id, actorId, new { category.Name }, ct);
        return await GetAsync(id, ct);
    }

    public async Task DeleteAsync(Guid id, Guid actorId, CancellationToken ct = default)
    {
        var category = await _db.Categories.Include(x => x.Blogs).FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("Category not found.");
        if (category.Blogs.Count != 0)
            throw new InvalidOperationException("Cannot delete a category that contains blogs.");

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync(ct);
        await _audit.WriteAsync(AuditAction.Delete, nameof(Category), id, actorId, new { category.Name }, ct);
    }
}
