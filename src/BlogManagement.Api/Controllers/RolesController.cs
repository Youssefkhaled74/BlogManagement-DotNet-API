using BlogManagement.Api.Security;
using BlogManagement.Application.DTOs;
using BlogManagement.Application.Interfaces;
using BlogManagement.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlogManagement.Api.Controllers;

[Authorize]
[Route("api/roles")]
public sealed class RolesController(IRoleService service) : ApiControllerBase
{
    [HttpGet, HasPermission(Permissions.RolesRead)]
    public async Task<ActionResult<IReadOnlyCollection<RoleDto>>> GetAll(CancellationToken ct) => Ok(await service.GetAllAsync(ct));

    [HttpGet("permissions"), HasPermission(Permissions.RolesRead)]
    public async Task<ActionResult<IReadOnlyCollection<PermissionDto>>> GetPermissions(CancellationToken ct) => Ok(await service.GetPermissionsAsync(ct));

    [HttpGet("{id:guid}"), HasPermission(Permissions.RolesRead)]
    public async Task<ActionResult<RoleDto>> Get(Guid id, CancellationToken ct) => Ok(await service.GetAsync(id, ct));

    [HttpPost, HasPermission(Permissions.RolesManage)]
    public async Task<ActionResult<RoleDto>> Create(CreateRoleDto request, CancellationToken ct)
    {
        var result = await service.CreateAsync(request, CurrentUserId, ct);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}"), HasPermission(Permissions.RolesManage)]
    public async Task<ActionResult<RoleDto>> Update(Guid id, UpdateRoleDto request, CancellationToken ct) =>
        Ok(await service.UpdateAsync(id, request, CurrentUserId, ct));

    [HttpPut("{id:guid}/permissions"), HasPermission(Permissions.RolesManage)]
    public async Task<ActionResult<RoleDto>> SetPermissions(Guid id, SetRolePermissionsDto request, CancellationToken ct) =>
        Ok(await service.SetPermissionsAsync(id, request.Permissions, CurrentUserId, ct));

    [HttpDelete("{id:guid}"), HasPermission(Permissions.RolesManage)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await service.DeleteAsync(id, CurrentUserId, ct);
        return NoContent();
    }
}
