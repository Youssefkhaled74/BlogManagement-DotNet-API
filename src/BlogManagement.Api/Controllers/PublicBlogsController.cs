using BlogManagement.Application.DTOs;
using BlogManagement.Application.Interfaces;
using BlogManagement.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlogManagement.Api.Controllers;

[AllowAnonymous]
[Route("api/public/blogs")]
public sealed class PublicBlogsController(IBlogService service) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<BlogDto>>> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null,
        CancellationToken ct = default) =>
        Ok(await service.GetAllAsync(Guid.Empty, false, page, pageSize, BlogStatus.Published, search, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BlogDto>> Get(Guid id, CancellationToken ct) =>
        Ok(await service.GetAsync(id, Guid.Empty, false, ct));
}
