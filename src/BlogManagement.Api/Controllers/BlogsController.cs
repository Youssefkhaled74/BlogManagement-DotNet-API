using BlogManagement.Api.Security;
using BlogManagement.Api.Uploads;
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
    [HttpPost("{id:guid}/image"), HasPermission(Permissions.BlogsUpdate)]
    [Consumes("multipart/form-data"), RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<ActionResult<BlogDto>> UploadImage(Guid id, [FromForm] ImageUploadRequest request,
        [FromServices] ImageStorage images, CancellationToken ct)
    {
        var blog = await service.GetAsync(id, CurrentUserId, HasPermission(Permissions.BlogsReview), ct);
        if (blog.AuthorId != CurrentUserId && !HasPermission(Permissions.BlogsReview)) return Forbid();
        if (blog.Status is BlogStatus.PendingApproval or BlogStatus.Published)
            throw new InvalidOperationException("A pending or published blog cannot be edited.");
        var url = await images.SaveAsync(request.File, ct);
        var result = await service.SetImageAsync(id, url, CurrentUserId, HasPermission(Permissions.BlogsReview), ct);
        images.Delete(blog.ImageUrl);
        return Ok(result);
    }

    [HttpDelete("{id:guid}/image"), HasPermission(Permissions.BlogsUpdate)]
    public async Task<ActionResult<BlogDto>> DeleteImage(Guid id, [FromServices] ImageStorage images, CancellationToken ct)
    {
        var blog = await service.GetAsync(id, CurrentUserId, HasPermission(Permissions.BlogsReview), ct);
        var result = await service.SetImageAsync(id, null, CurrentUserId, HasPermission(Permissions.BlogsReview), ct);
        images.Delete(blog.ImageUrl);
        return Ok(result);
    }
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
    public async Task<IActionResult> Delete(Guid id, [FromServices] ImageStorage images, CancellationToken ct)
    {
        var blog = await service.GetAsync(id, CurrentUserId, HasPermission(Permissions.BlogsReview), ct);
        await service.DeleteAsync(id, CurrentUserId, HasPermission(Permissions.BlogsReview), ct);
        images.Delete(blog.ImageUrl);
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
