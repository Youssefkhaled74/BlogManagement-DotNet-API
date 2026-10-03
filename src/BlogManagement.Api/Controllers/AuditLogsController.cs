using BlogManagement.Api.Security;
using BlogManagement.Application.DTOs;
using BlogManagement.Application.Interfaces;
using BlogManagement.Application.Security;
using BlogManagement.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlogManagement.Api.Controllers;

[Authorize]
[Route("api/audit-logs")]
public sealed class AuditLogsController(IAuditService service) : ApiControllerBase
{
    [HttpGet, HasPermission(Permissions.AuditRead)]
    public async Task<ActionResult<PagedResult<AuditLogDto>>> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? entityName = null, [FromQuery] AuditAction? action = null,
        CancellationToken ct = default) => Ok(await service.GetAllAsync(page, pageSize, entityName, action, ct));
}
