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
[Route("api/projects")]
public sealed class ProjectsController(AppDbContext db, IProjectAccessService access, IFileStorage storage) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<ProjectResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var query = db.Projects.AsNoTracking();
        if (!User.IsAdmin())
        {
            query = query.Where(x => x.Members.Any(m => m.UserId == userId));
        }

        var projects = await query.OrderBy(x => x.Name).ToListAsync(cancellationToken);
        return Ok(projects.Select(x => x.ToResponse()).ToArray());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProjectResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (!await access.CanReadAsync(id, User.GetUserId(), User.IsAdmin(), cancellationToken))
        {
            return Forbid();
        }

        var project = await db.Projects.AsNoTracking()
            .Include(x => x.Members).ThenInclude(x => x.User)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        return project is null ? NotFound() : Ok(project.ToResponse(includeMembers: true));
    }

    [HttpPost]
    [ProducesResponseType<ProjectResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ProjectResponse>> Create(CreateProjectRequest request, CancellationToken cancellationToken)
    {
        var key = request.Key.Trim().ToUpperInvariant();
        if (await db.Projects.AnyAsync(x => x.Key == key, cancellationToken))
        {
            return Conflict(new ProblemDetails { Title = "Project key already exists." });
        }

        var userId = User.GetUserId();
        var project = new Project
        {
            Name = request.Name.Trim(),
            Key = key,
            Description = request.Description?.Trim(),
            CreatedById = userId
        };
        project.Members.Add(new ProjectMember { Project = project, UserId = userId, Role = ProjectRole.Owner });

        db.Projects.Add(project);
        await db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = project.Id }, project.ToResponse());
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProjectResponse>> Update(Guid id, UpdateProjectRequest request, CancellationToken cancellationToken)
    {
        if (!await access.CanManageAsync(id, User.GetUserId(), User.IsAdmin(), cancellationToken))
        {
            return Forbid();
        }

        var project = await db.Projects.FindAsync([id], cancellationToken);
        if (project is null)
        {
            return NotFound();
        }

        project.Name = request.Name.Trim();
        project.Description = request.Description?.Trim();
        project.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(project.ToResponse());
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!await access.CanManageAsync(id, User.GetUserId(), User.IsAdmin(), cancellationToken))
        {
            return Forbid();
        }

        var project = await db.Projects.FindAsync([id], cancellationToken);
        if (project is null)
        {
            return NotFound();
        }

        var storedNames = await db.Attachments
            .Where(x => x.WorkItem.ProjectId == id)
            .Select(x => x.StoredName)
            .ToListAsync(cancellationToken);
        db.Projects.Remove(project);
        await db.SaveChangesAsync(cancellationToken);
        foreach (var storedName in storedNames)
        {
            storage.Delete(storedName);
        }

        return NoContent();
    }

    [HttpPost("{id:guid}/members")]
    public async Task<ActionResult<ProjectMemberResponse>> AddMember(Guid id, AddProjectMemberRequest request, CancellationToken cancellationToken)
    {
        if (!await access.CanManageAsync(id, User.GetUserId(), User.IsAdmin(), cancellationToken))
        {
            return Forbid();
        }

        if (request.Role == ProjectRole.Owner)
        {
            return BadRequest(new ProblemDetails { Title = "Ownership transfer is not supported by this endpoint." });
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var member = await db.Users.SingleOrDefaultAsync(x => x.Email == email, cancellationToken);
        if (member is null)
        {
            return NotFound(new ProblemDetails { Title = "User not found." });
        }

        if (await db.ProjectMembers.AnyAsync(x => x.ProjectId == id && x.UserId == member.Id, cancellationToken))
        {
            return Conflict(new ProblemDetails { Title = "User is already a project member." });
        }

        var membership = new ProjectMember { ProjectId = id, UserId = member.Id, Role = request.Role };
        db.ProjectMembers.Add(membership);
        await db.SaveChangesAsync(cancellationToken);
        return Created($"/api/projects/{id}", new ProjectMemberResponse(member.Id, member.Name, member.Email, membership.Role, membership.JoinedAt));
    }

    [HttpDelete("{id:guid}/members/{userId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        if (!await access.CanManageAsync(id, User.GetUserId(), User.IsAdmin(), cancellationToken))
        {
            return Forbid();
        }

        var membership = await db.ProjectMembers.FindAsync([id, userId], cancellationToken);
        if (membership is null)
        {
            return NotFound();
        }

        if (membership.Role == ProjectRole.Owner)
        {
            return BadRequest(new ProblemDetails { Title = "The project owner cannot be removed." });
        }

        db.ProjectMembers.Remove(membership);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
