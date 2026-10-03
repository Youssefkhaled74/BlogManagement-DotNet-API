using BlogManagement.Application.DTOs;
using BlogManagement.Application.Interfaces;
using BlogManagement.Domain.Entities;
using BlogManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BlogManagement.Infrastructure.Services;

public sealed class EmployeeService : IEmployeeService
{
    private readonly BlogManagementDbContext _db;
    private readonly IAuditService _audit;
    public EmployeeService(BlogManagementDbContext db, IAuditService audit) => (_db, _audit) = (db, audit);

    public async Task<PagedResult<EmployeeDto>> GetAllAsync(int page, int pageSize, string? search, CancellationToken ct = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = _db.Employees.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.FirstName.Contains(term) || x.LastName.Contains(term) ||
                (x.EmployeeNumber != null && x.EmployeeNumber.Contains(term)));
        }
        var total = await query.CountAsync(ct);
        var items = await Map(query.OrderBy(x => x.FirstName).ThenBy(x => x.LastName))
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new PagedResult<EmployeeDto>(items, page, pageSize, total);
    }

    public async Task<EmployeeDto> GetAsync(Guid id, CancellationToken ct = default) =>
        await Map(_db.Employees.AsNoTracking().Where(x => x.Id == id)).FirstOrDefaultAsync(ct)
        ?? throw new KeyNotFoundException("Employee not found.");

    public async Task<EmployeeDto> CreateAsync(CreateEmployeeDto request, Guid actorId, CancellationToken ct = default)
    {
        await ValidateReferencesAsync(request.EmployeeNumber, request.UserId, null, ct);
        var employee = new Employee
        {
            Id = Guid.NewGuid(), FirstName = request.FirstName.Trim(), LastName = request.LastName.Trim(),
            EmployeeNumber = NullIfEmpty(request.EmployeeNumber), Department = NullIfEmpty(request.Department),
            JobTitle = NullIfEmpty(request.JobTitle), Title = NullIfEmpty(request.JobTitle), UserId = request.UserId,
            IsActive = true, CreatedAt = DateTime.UtcNow
        };
        _db.Employees.Add(employee);
        await _db.SaveChangesAsync(ct);
        await _audit.WriteAsync(AuditAction.Create, nameof(Employee), employee.Id, actorId, new { employee.EmployeeNumber }, ct);
        return await GetAsync(employee.Id, ct);
    }

    public async Task<EmployeeDto> UpdateAsync(Guid id, UpdateEmployeeDto request, Guid actorId, CancellationToken ct = default)
    {
        var employee = await _db.Employees.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("Employee not found.");
        await ValidateReferencesAsync(request.EmployeeNumber, request.UserId, id, ct);
        employee.FirstName = request.FirstName.Trim();
        employee.LastName = request.LastName.Trim();
        employee.EmployeeNumber = NullIfEmpty(request.EmployeeNumber);
        employee.Department = NullIfEmpty(request.Department);
        employee.JobTitle = NullIfEmpty(request.JobTitle);
        employee.Title = employee.JobTitle;
        employee.UserId = request.UserId;
        employee.IsActive = request.IsActive;
        employee.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        await _audit.WriteAsync(AuditAction.Update, nameof(Employee), id, actorId, null, ct);
        return await GetAsync(id, ct);
    }

    public async Task DeleteAsync(Guid id, Guid actorId, CancellationToken ct = default)
    {
        var employee = await _db.Employees.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("Employee not found.");
        _db.Employees.Remove(employee);
        await _db.SaveChangesAsync(ct);
        await _audit.WriteAsync(AuditAction.Delete, nameof(Employee), id, actorId, new { employee.EmployeeNumber }, ct);
    }

    private async Task ValidateReferencesAsync(string? employeeNumber, Guid? userId, Guid? currentId, CancellationToken ct)
    {
        var number = NullIfEmpty(employeeNumber);
        if (number is not null && await _db.Employees.AnyAsync(x => (!currentId.HasValue || x.Id != currentId.Value) && x.EmployeeNumber == number, ct))
            throw new InvalidOperationException("Employee number already exists.");
        if (userId.HasValue && !await _db.Users.AnyAsync(x => x.Id == userId.Value, ct))
            throw new InvalidOperationException("Selected user does not exist.");
        if (userId.HasValue && await _db.Employees.AnyAsync(x => (!currentId.HasValue || x.Id != currentId.Value) && x.UserId == userId, ct))
            throw new InvalidOperationException("Selected user is already linked to an employee.");
    }

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static IQueryable<EmployeeDto> Map(IQueryable<Employee> query) => query.Select(x => new EmployeeDto(
        x.Id, x.FirstName, x.LastName, x.EmployeeNumber, x.Department, x.JobTitle,
        x.UserId, x.IsActive, x.CreatedAt, x.UpdatedAt));
}
