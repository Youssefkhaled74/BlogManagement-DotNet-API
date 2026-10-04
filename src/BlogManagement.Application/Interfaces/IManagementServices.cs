using BlogManagement.Application.DTOs;
using BlogManagement.Domain.Entities;

namespace BlogManagement.Application.Interfaces;

public interface ICategoryService
{
    Task<IReadOnlyCollection<CategoryDto>> GetAllAsync(CancellationToken ct = default);
    Task<CategoryDto> GetAsync(Guid id, CancellationToken ct = default);
    Task<CategoryDto> CreateAsync(CreateCategoryDto request, Guid actorId, CancellationToken ct = default);
    Task<CategoryDto> UpdateAsync(Guid id, UpdateCategoryDto request, Guid actorId, CancellationToken ct = default);
    Task DeleteAsync(Guid id, Guid actorId, CancellationToken ct = default);
}

public interface IBlogService
{
    Task<BlogDto> SetImageAsync(Guid id, string? imageUrl, Guid actorId, bool canManageAll, CancellationToken ct = default);
    Task<PagedResult<BlogDto>> GetAllAsync(Guid userId, bool canReviewAll, int page, int pageSize, BlogStatus? status, string? search, CancellationToken ct = default);
    Task<BlogDto> GetAsync(Guid id, Guid userId, bool canReviewAll, CancellationToken ct = default);
    Task<BlogDto> CreateAsync(CreateBlogDto request, Guid actorId, CancellationToken ct = default);
    Task<BlogDto> UpdateAsync(Guid id, UpdateBlogDto request, Guid actorId, bool canManageAll, CancellationToken ct = default);
    Task DeleteAsync(Guid id, Guid actorId, bool canManageAll, CancellationToken ct = default);
    Task<BlogDto> SubmitAsync(Guid id, Guid actorId, CancellationToken ct = default);
    Task<BlogDto> ReviewAsync(Guid id, ReviewBlogDto request, Guid actorId, CancellationToken ct = default);
    Task<BlogDto> PublishAsync(Guid id, Guid actorId, CancellationToken ct = default);
    Task<BlogDto> UnpublishAsync(Guid id, Guid actorId, CancellationToken ct = default);
    Task<IReadOnlyCollection<BlogApprovalHistoryDto>> GetHistoryAsync(Guid id, CancellationToken ct = default);
}

public interface IUserService
{
    Task<UserSummaryDto> SetProfileImageAsync(Guid id, string? imageUrl, CancellationToken ct = default);
    Task<PagedResult<UserSummaryDto>> GetAllAsync(int page, int pageSize, string? search, CancellationToken ct = default);
    Task<UserSummaryDto> GetAsync(Guid id, CancellationToken ct = default);
    Task<UserSummaryDto> UpdateAsync(Guid id, UpdateUserDto request, Guid actorId, CancellationToken ct = default);
    Task<UserSummaryDto> SetStatusAsync(Guid id, bool isActive, Guid actorId, CancellationToken ct = default);
    Task<UserSummaryDto> SetRolesAsync(Guid id, IReadOnlyCollection<Guid> roleIds, Guid actorId, CancellationToken ct = default);
}

public interface IRoleService
{
    Task<IReadOnlyCollection<RoleDto>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyCollection<PermissionDto>> GetPermissionsAsync(CancellationToken ct = default);
    Task<RoleDto> GetAsync(Guid id, CancellationToken ct = default);
    Task<RoleDto> CreateAsync(CreateRoleDto request, Guid actorId, CancellationToken ct = default);
    Task<RoleDto> UpdateAsync(Guid id, UpdateRoleDto request, Guid actorId, CancellationToken ct = default);
    Task<RoleDto> SetPermissionsAsync(Guid id, IReadOnlyCollection<string> permissions, Guid actorId, CancellationToken ct = default);
    Task DeleteAsync(Guid id, Guid actorId, CancellationToken ct = default);
}

public interface IEmployeeService
{
    Task<PagedResult<EmployeeDto>> GetAllAsync(int page, int pageSize, string? search, CancellationToken ct = default);
    Task<EmployeeDto> GetAsync(Guid id, CancellationToken ct = default);
    Task<EmployeeDto> CreateAsync(CreateEmployeeDto request, Guid actorId, CancellationToken ct = default);
    Task<EmployeeDto> UpdateAsync(Guid id, UpdateEmployeeDto request, Guid actorId, CancellationToken ct = default);
    Task DeleteAsync(Guid id, Guid actorId, CancellationToken ct = default);
}

public interface IAuditService
{
    Task WriteAsync(AuditAction action, string entityName, Guid entityId, Guid actorId, object? data = null, CancellationToken ct = default);
    Task<PagedResult<AuditLogDto>> GetAllAsync(int page, int pageSize, string? entityName, AuditAction? action, CancellationToken ct = default);
}
