using BlogManagement.Api.Security;
using BlogManagement.Application.DTOs;
using BlogManagement.Application.Interfaces;
using BlogManagement.Application.Security;
using BlogManagement.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlogManagement.Api.Controllers;

[Authorize]
[Route("api/blogs")]
public sealed class BlogsController(IBlogService service) : ApiControllerBase
{
    [HttpGet, HasPermission(Permissions.BlogsRead)]
    public async Task<ActionResult<PagedResult<BlogDto>>> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] BlogStatus? status = null, [FromQuery] string? search = null,
        CancellationToken ct = default) =>
        Ok(await service.GetAllAsync(CurrentUserId, HasPermission(Permissions.BlogsReview), page, pageSize, status, search, ct));

    [HttpGet("{id:guid}"), HasPermission(Permissions.BlogsRead)]
    public async Task<ActionResult<BlogDto>> Get(Guid id, CancellationToken ct) =>
        Ok(await service.GetAsync(id, CurrentUserId, HasPermission(Permissions.BlogsReview), ct));

    [HttpPost, HasPermission(Permissions.BlogsCreate)]
    public async Task<ActionResult<BlogDto>> Create(CreateBlogDto request, CancellationToken ct)
    {
        var result = await service.CreateAsync(request, CurrentUserId, ct);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}"), HasPermission(Permissions.BlogsUpdate)]
    public async Task<ActionResult<BlogDto>> Update(Guid id, UpdateBlogDto request, CancellationToken ct) =>
        Ok(await service.UpdateAsync(id, request, CurrentUserId, HasPermission(Permissions.BlogsReview), ct));

    [HttpDelete("{id:guid}"), HasPermission(Permissions.BlogsDelete)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await service.DeleteAsync(id, CurrentUserId, HasPermission(Permissions.BlogsReview), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/submit"), HasPermission(Permissions.BlogsSubmit)]
    public async Task<ActionResult<BlogDto>> Submit(Guid id, CancellationToken ct) => Ok(await service.SubmitAsync(id, CurrentUserId, ct));

    [HttpPost("{id:guid}/review"), HasPermission(Permissions.BlogsReview)]
    public async Task<ActionResult<BlogDto>> Review(Guid id, ReviewBlogDto request, CancellationToken ct) =>
        Ok(await service.ReviewAsync(id, request, CurrentUserId, ct));

    [HttpPost("{id:guid}/publish"), HasPermission(Permissions.BlogsPublish)]
    public async Task<ActionResult<BlogDto>> Publish(Guid id, CancellationToken ct) => Ok(await service.PublishAsync(id, CurrentUserId, ct));

    [HttpPost("{id:guid}/unpublish"), HasPermission(Permissions.BlogsPublish)]
    public async Task<ActionResult<BlogDto>> Unpublish(Guid id, CancellationToken ct) => Ok(await service.UnpublishAsync(id, CurrentUserId, ct));

    [HttpGet("{id:guid}/history"), HasPermission(Permissions.BlogsReview)]
    public async Task<ActionResult<IReadOnlyCollection<BlogApprovalHistoryDto>>> History(Guid id, CancellationToken ct) =>
        Ok(await service.GetHistoryAsync(id, ct));
}
