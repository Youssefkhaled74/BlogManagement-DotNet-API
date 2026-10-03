using BlogManagement.Api.Security;
using BlogManagement.Application.DTOs;
using BlogManagement.Application.Interfaces;
using BlogManagement.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlogManagement.Api.Controllers;

[Authorize]
[Route("api/users")]
public sealed class UsersController(IUserService service) : ApiControllerBase
{
    [HttpGet, HasPermission(Permissions.UsersRead)]
    public async Task<ActionResult<PagedResult<UserSummaryDto>>> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null,
        CancellationToken ct = default) => Ok(await service.GetAllAsync(page, pageSize, search, ct));

    [HttpGet("{id:guid}"), HasPermission(Permissions.UsersRead)]
    public async Task<ActionResult<UserSummaryDto>> Get(Guid id, CancellationToken ct) => Ok(await service.GetAsync(id, ct));

    [HttpPut("{id:guid}"), HasPermission(Permissions.UsersManage)]
    public async Task<ActionResult<UserSummaryDto>> Update(Guid id, UpdateUserDto request, CancellationToken ct) =>
        Ok(await service.UpdateAsync(id, request, CurrentUserId, ct));

    [HttpPatch("{id:guid}/status"), HasPermission(Permissions.UsersManage)]
    public async Task<ActionResult<UserSummaryDto>> SetStatus(Guid id, SetUserStatusDto request, CancellationToken ct) =>
        Ok(await service.SetStatusAsync(id, request.IsActive, CurrentUserId, ct));

    [HttpPut("{id:guid}/roles"), HasPermission(Permissions.UsersManage)]
    public async Task<ActionResult<UserSummaryDto>> SetRoles(Guid id, SetUserRolesDto request, CancellationToken ct) =>
        Ok(await service.SetRolesAsync(id, request.RoleIds, CurrentUserId, ct));
}
