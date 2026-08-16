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
public sealed class WorkItemsController(AppDbContext db, IProjectAccessService access, IFileStorage storage) : ControllerBase
{
    [HttpGet("projects/{projectId:guid}/work-items")]
    public async Task<ActionResult<IReadOnlyCollection<WorkItemResponse>>> GetAll(Guid projectId, CancellationToken cancellationToken)
    {
        if (!await access.CanReadAsync(projectId, User.GetUserId(), User.IsAdmin(), cancellationToken))
        {
            return Forbid();
        }

        var items = await db.WorkItems.AsNoTracking()
            .Where(x => x.ProjectId == projectId)
            .OrderBy(x => x.Status).ThenByDescending(x => x.Priority).ThenByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
        return Ok(items.Select(x => x.ToResponse()).ToArray());
    }

    [HttpGet("work-items/{id:guid}")]
    public async Task<ActionResult<WorkItemResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var item = await db.WorkItems.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        return await access.CanReadAsync(item.ProjectId, User.GetUserId(), User.IsAdmin(), cancellationToken)
            ? Ok(item.ToResponse())
            : Forbid();
    }

    [HttpPost("projects/{projectId:guid}/work-items")]
    public async Task<ActionResult<WorkItemResponse>> Create(Guid projectId, CreateWorkItemRequest request, CancellationToken cancellationToken)
    {
        if (!await access.CanContributeAsync(projectId, User.GetUserId(), User.IsAdmin(), cancellationToken))
        {
            return Forbid();
        }

        if (!await IsValidAssigneeAsync(projectId, request.AssigneeId, cancellationToken))
        {
            return BadRequest(new ProblemDetails { Title = "Assignee must be a project member." });
        }

        var item = new WorkItem
        {
            ProjectId = projectId,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            Priority = request.Priority,
            AssigneeId = request.AssigneeId,
            DueAt = request.DueAt,
            CreatedById = User.GetUserId()
        };

        db.WorkItems.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, item.ToResponse());
    }

    [HttpPut("work-items/{id:guid}")]
    public async Task<ActionResult<WorkItemResponse>> Update(Guid id, UpdateWorkItemRequest request, CancellationToken cancellationToken)
    {
        var item = await db.WorkItems.FindAsync([id], cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        if (!await access.CanContributeAsync(item.ProjectId, User.GetUserId(), User.IsAdmin(), cancellationToken))
        {
            return Forbid();
        }

        if (!await IsValidAssigneeAsync(item.ProjectId, request.AssigneeId, cancellationToken))
        {
            return BadRequest(new ProblemDetails { Title = "Assignee must be a project member." });
        }

        item.Title = request.Title.Trim();
        item.Description = request.Description?.Trim();
        item.Status = request.Status;
        item.Priority = request.Priority;
        item.AssigneeId = request.AssigneeId;
        item.DueAt = request.DueAt;
        item.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(item.ToResponse());
    }

    [HttpDelete("work-items/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var item = await db.WorkItems.FindAsync([id], cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        if (!await access.CanManageAsync(item.ProjectId, User.GetUserId(), User.IsAdmin(), cancellationToken))
        {
            return Forbid();
        }

        var storedNames = await db.Attachments
            .Where(x => x.WorkItemId == id)
            .Select(x => x.StoredName)
            .ToListAsync(cancellationToken);
        db.WorkItems.Remove(item);
        await db.SaveChangesAsync(cancellationToken);
        foreach (var storedName in storedNames)
        {
            storage.Delete(storedName);
        }

        return NoContent();
    }

    private Task<bool> IsValidAssigneeAsync(Guid projectId, Guid? assigneeId, CancellationToken cancellationToken) =>
        assigneeId is null
            ? Task.FromResult(true)
            : db.ProjectMembers.AnyAsync(x => x.ProjectId == projectId && x.UserId == assigneeId, cancellationToken);
}
