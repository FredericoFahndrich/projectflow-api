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
public sealed class AttachmentsController(AppDbContext db, IProjectAccessService access, IFileStorage storage) : ControllerBase
{
    [HttpPost("work-items/{workItemId:guid}/attachments")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<AttachmentResponse>> Upload(Guid workItemId, IFormFile file, CancellationToken cancellationToken)
    {
        var projectId = await db.WorkItems.Where(x => x.Id == workItemId)
            .Select(x => (Guid?)x.ProjectId)
            .SingleOrDefaultAsync(cancellationToken);
        if (projectId is null)
        {
            return NotFound();
        }

        if (!await access.CanContributeAsync(projectId.Value, User.GetUserId(), User.IsAdmin(), cancellationToken))
        {
            return Forbid();
        }

        StoredFile stored;
        try
        {
            stored = await storage.SaveAsync(file, cancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new ProblemDetails { Title = exception.Message });
        }

        var attachment = new Attachment
        {
            WorkItemId = workItemId,
            UploadedById = User.GetUserId(),
            OriginalName = Path.GetFileName(file.FileName),
            StoredName = stored.StoredName,
            ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
            SizeBytes = stored.SizeBytes
        };

        db.Attachments.Add(attachment);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            storage.Delete(stored.StoredName);
            throw;
        }

        return Created($"/api/attachments/{attachment.Id}/download", attachment.ToResponse());
    }

    [HttpGet("attachments/{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id, CancellationToken cancellationToken)
    {
        var attachment = await db.Attachments.AsNoTracking()
            .Include(x => x.WorkItem)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (attachment is null)
        {
            return NotFound();
        }

        if (!await access.CanReadAsync(attachment.WorkItem.ProjectId, User.GetUserId(), User.IsAdmin(), cancellationToken))
        {
            return Forbid();
        }

        try
        {
            return File(storage.OpenRead(attachment.StoredName), attachment.ContentType, attachment.OriginalName);
        }
        catch (FileNotFoundException)
        {
            return NotFound(new ProblemDetails { Title = "Attachment content was not found." });
        }
    }

    [HttpDelete("attachments/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var attachment = await db.Attachments.Include(x => x.WorkItem).SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (attachment is null)
        {
            return NotFound();
        }

        var userId = User.GetUserId();
        var canDelete = attachment.UploadedById == userId ||
            await access.CanManageAsync(attachment.WorkItem.ProjectId, userId, User.IsAdmin(), cancellationToken);
        if (!canDelete)
        {
            return Forbid();
        }

        db.Attachments.Remove(attachment);
        await db.SaveChangesAsync(cancellationToken);
        storage.Delete(attachment.StoredName);
        return NoContent();
    }
}

