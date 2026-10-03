using System.ComponentModel.DataAnnotations;
using BlogManagement.Domain.Entities;

namespace BlogManagement.Application.DTOs;

public sealed record PermissionDto(Guid Id, string Name, string? Description);

public sealed record RoleDto(Guid Id, string Name, string? Description, IReadOnlyCollection<string> Permissions);

public sealed record CreateRoleDto(
    [Required, MinLength(2), MaxLength(80)] string Name,
    [MaxLength(300)] string? Description,
    IReadOnlyCollection<string>? Permissions);

public sealed record UpdateRoleDto(
    [Required, MinLength(2), MaxLength(80)] string Name,
    [MaxLength(300)] string? Description);

public sealed record SetRolePermissionsDto([Required] IReadOnlyCollection<string> Permissions);

public sealed record UserSummaryDto(
    Guid Id,
    string Username,
    string Email,
    string? DisplayName,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? LastLoginAt,
    IReadOnlyCollection<string> Roles);

public sealed record UpdateUserDto(
    [Required, MinLength(2), MaxLength(80)] string Username,
    [Required, EmailAddress] string Email,
    [MaxLength(120)] string? DisplayName);

public sealed record SetUserStatusDto(bool IsActive);
public sealed record SetUserRolesDto([Required] IReadOnlyCollection<Guid> RoleIds);

public sealed record CategoryDto(Guid Id, string Name, string? Description, int BlogsCount);

public sealed record CreateCategoryDto(
    [Required, MinLength(2), MaxLength(120)] string Name,
    [MaxLength(500)] string? Description);

public sealed record UpdateCategoryDto(
    [Required, MinLength(2), MaxLength(120)] string Name,
    [MaxLength(500)] string? Description);

public sealed record BlogDto(
    Guid Id,
    string Title,
    string Slug,
    string Content,
    BlogStatus Status,
    Guid AuthorId,
    string AuthorName,
    Guid CategoryId,
    string CategoryName,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    DateTime? PublishedAt,
    string? ReviewComment);

public sealed record CreateBlogDto(
    [Required, MinLength(3), MaxLength(250)] string Title,
    [Required, MinLength(10)] string Content,
    Guid CategoryId,
    [MaxLength(250)] string? Slug);

public sealed record UpdateBlogDto(
    [Required, MinLength(3), MaxLength(250)] string Title,
    [Required, MinLength(10)] string Content,
    Guid CategoryId,
    [MaxLength(250)] string? Slug);

public sealed record ReviewBlogDto(bool Approved, [MaxLength(1000)] string? Comment);

public sealed record BlogApprovalHistoryDto(
    Guid Id,
    BlogStatus Decision,
    Guid ReviewedByUserId,
    string ReviewedBy,
    string? Comment,
    DateTime Timestamp);

public sealed record EmployeeDto(
    Guid Id,
    string FirstName,
    string LastName,
    string? EmployeeNumber,
    string? Department,
    string? JobTitle,
    Guid? UserId,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record CreateEmployeeDto(
    [Required, MaxLength(100)] string FirstName,
    [Required, MaxLength(100)] string LastName,
    [MaxLength(50)] string? EmployeeNumber,
    [MaxLength(100)] string? Department,
    [MaxLength(100)] string? JobTitle,
    Guid? UserId);

public sealed record UpdateEmployeeDto(
    [Required, MaxLength(100)] string FirstName,
    [Required, MaxLength(100)] string LastName,
    [MaxLength(50)] string? EmployeeNumber,
    [MaxLength(100)] string? Department,
    [MaxLength(100)] string? JobTitle,
    Guid? UserId,
    bool IsActive);

public sealed record AuditLogDto(
    Guid Id,
    AuditAction Action,
    string EntityName,
    Guid EntityId,
    Guid PerformedByUserId,
    string? PerformedBy,
    DateTime Timestamp,
    string? Data);
