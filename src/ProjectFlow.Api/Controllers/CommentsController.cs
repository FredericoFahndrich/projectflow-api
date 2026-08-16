using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectFlow.Api.Contracts;
using ProjectFlow.Api.Data;
using ProjectFlow.Api.Domain;
using ProjectFlow.Api.Infrastructure;

namespace ProjectFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public sealed class CommentsController(AppDbContext db, IProjectAccessService access) : ControllerBase
{
    [HttpGet("work-items/{workItemId:guid}/comments")]
    public async Task<ActionResult<IReadOnlyCollection<CommentResponse>>> GetAll(Guid workItemId, CancellationToken cancellationToken)
    {
        var projectId = await GetProjectIdAsync(workItemId, cancellationToken);
        if (projectId is null)
        {
            return NotFound();
        }

        if (!await access.CanReadAsync(projectId.Value, User.GetUserId(), User.IsAdmin(), cancellationToken))
        {
            return Forbid();
        }

        var comments = await db.Comments.AsNoTracking()
            .Include(x => x.Author)
            .Where(x => x.WorkItemId == workItemId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
        return Ok(comments.Select(x => x.ToResponse()).ToArray());
    }

    [HttpPost("work-items/{workItemId:guid}/comments")]
    public async Task<ActionResult<CommentResponse>> Create(Guid workItemId, CreateCommentRequest request, CancellationToken cancellationToken)
    {
        var projectId = await GetProjectIdAsync(workItemId, cancellationToken);
        if (projectId is null)
        {
            return NotFound();
        }

        if (!await access.CanContributeAsync(projectId.Value, User.GetUserId(), User.IsAdmin(), cancellationToken))
        {
            return Forbid();
        }

        var comment = new Comment
        {
            WorkItemId = workItemId,
            AuthorId = User.GetUserId(),
            Body = request.Body.Trim()
        };
        db.Comments.Add(comment);
        await db.SaveChangesAsync(cancellationToken);
        await db.Entry(comment).Reference(x => x.Author).LoadAsync(cancellationToken);
        return Created($"/api/work-items/{workItemId}/comments", comment.ToResponse());
    }

    [HttpDelete("comments/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var comment = await db.Comments.Include(x => x.WorkItem).SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (comment is null)
        {
            return NotFound();
        }

        var userId = User.GetUserId();
        var canDelete = comment.AuthorId == userId ||
            await access.CanManageAsync(comment.WorkItem.ProjectId, userId, User.IsAdmin(), cancellationToken);
        if (!canDelete)
        {
            return Forbid();
        }

        db.Comments.Remove(comment);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private Task<Guid?> GetProjectIdAsync(Guid workItemId, CancellationToken cancellationToken) =>
        db.WorkItems.Where(x => x.Id == workItemId).Select(x => (Guid?)x.ProjectId).SingleOrDefaultAsync(cancellationToken);
}

