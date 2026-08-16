using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectFlow.Api.Contracts;
using ProjectFlow.Api.Data;
using ProjectFlow.Api.Infrastructure;

namespace ProjectFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public sealed class UsersController(AppDbContext db) : ControllerBase
{
    [HttpGet("me")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<UserResponse>> GetMe(CancellationToken cancellationToken)
    {
        var user = await db.Users.FindAsync([User.GetUserId()], cancellationToken);
        return user is null ? NotFound() : Ok(user.ToResponse());
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IReadOnlyCollection<UserResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var users = await db.Users.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken);
        return Ok(users.Select(x => x.ToResponse()).ToArray());
    }

    [HttpPatch("{id:guid}/role")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UserResponse>> UpdateRole(Guid id, UpdateUserRoleRequest request, CancellationToken cancellationToken)
    {
        var user = await db.Users.FindAsync([id], cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        if (id == User.GetUserId() && request.Role != user.Role)
        {
            return BadRequest(new ProblemDetails { Title = "Administrators cannot change their own global role." });
        }

        user.Role = request.Role;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(user.ToResponse());
    }
}

