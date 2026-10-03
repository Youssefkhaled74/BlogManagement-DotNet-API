using BlogManagement.Api.Security;
using BlogManagement.Application.DTOs;
using BlogManagement.Application.Interfaces;
using BlogManagement.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlogManagement.Api.Controllers;

[Authorize]
[Route("api/categories")]
public sealed class CategoriesController(ICategoryService service) : ApiControllerBase
{
    [HttpGet, HasPermission(Permissions.CategoriesRead)]
    public async Task<ActionResult<IReadOnlyCollection<CategoryDto>>> GetAll(CancellationToken ct) => Ok(await service.GetAllAsync(ct));

    [HttpGet("{id:guid}"), HasPermission(Permissions.CategoriesRead)]
    public async Task<ActionResult<CategoryDto>> Get(Guid id, CancellationToken ct) => Ok(await service.GetAsync(id, ct));

    [HttpPost, HasPermission(Permissions.CategoriesManage)]
    public async Task<ActionResult<CategoryDto>> Create(CreateCategoryDto request, CancellationToken ct)
    {
        var result = await service.CreateAsync(request, CurrentUserId, ct);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}"), HasPermission(Permissions.CategoriesManage)]
    public async Task<ActionResult<CategoryDto>> Update(Guid id, UpdateCategoryDto request, CancellationToken ct) =>
        Ok(await service.UpdateAsync(id, request, CurrentUserId, ct));

    [HttpDelete("{id:guid}"), HasPermission(Permissions.CategoriesManage)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await service.DeleteAsync(id, CurrentUserId, ct);
        return NoContent();
    }
}
