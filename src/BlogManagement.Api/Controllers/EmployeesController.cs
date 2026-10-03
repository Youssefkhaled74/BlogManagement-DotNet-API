using BlogManagement.Api.Security;
using BlogManagement.Application.DTOs;
using BlogManagement.Application.Interfaces;
using BlogManagement.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlogManagement.Api.Controllers;

[Authorize]
[Route("api/employees")]
public sealed class EmployeesController(IEmployeeService service) : ApiControllerBase
{
    [HttpGet, HasPermission(Permissions.EmployeesRead)]
    public async Task<ActionResult<PagedResult<EmployeeDto>>> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null,
        CancellationToken ct = default) => Ok(await service.GetAllAsync(page, pageSize, search, ct));

    [HttpGet("{id:guid}"), HasPermission(Permissions.EmployeesRead)]
    public async Task<ActionResult<EmployeeDto>> Get(Guid id, CancellationToken ct) => Ok(await service.GetAsync(id, ct));

    [HttpPost, HasPermission(Permissions.EmployeesManage)]
    public async Task<ActionResult<EmployeeDto>> Create(CreateEmployeeDto request, CancellationToken ct)
    {
        var result = await service.CreateAsync(request, CurrentUserId, ct);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}"), HasPermission(Permissions.EmployeesManage)]
    public async Task<ActionResult<EmployeeDto>> Update(Guid id, UpdateEmployeeDto request, CancellationToken ct) =>
        Ok(await service.UpdateAsync(id, request, CurrentUserId, ct));

    [HttpDelete("{id:guid}"), HasPermission(Permissions.EmployeesManage)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await service.DeleteAsync(id, CurrentUserId, ct);
        return NoContent();
    }
}
