using Microsoft.EntityFrameworkCore;
using ProjectFlow.Api.Data;
using ProjectFlow.Api.Domain;

namespace ProjectFlow.Api.Infrastructure;

public interface IProjectAccessService
{
    Task<bool> CanReadAsync(Guid projectId, Guid userId, bool isAdmin, CancellationToken cancellationToken);
    Task<bool> CanContributeAsync(Guid projectId, Guid userId, bool isAdmin, CancellationToken cancellationToken);
    Task<bool> CanManageAsync(Guid projectId, Guid userId, bool isAdmin, CancellationToken cancellationToken);
}

public sealed class ProjectAccessService(AppDbContext db) : IProjectAccessService
{
    public Task<bool> CanReadAsync(Guid projectId, Guid userId, bool isAdmin, CancellationToken cancellationToken) =>
        isAdmin
            ? Task.FromResult(true)
            : db.ProjectMembers.AnyAsync(x => x.ProjectId == projectId && x.UserId == userId, cancellationToken);

    public Task<bool> CanContributeAsync(Guid projectId, Guid userId, bool isAdmin, CancellationToken cancellationToken) =>
        isAdmin
            ? Task.FromResult(true)
            : db.ProjectMembers.AnyAsync(
                x => x.ProjectId == projectId && x.UserId == userId && x.Role != ProjectRole.Viewer,
                cancellationToken);

    public Task<bool> CanManageAsync(Guid projectId, Guid userId, bool isAdmin, CancellationToken cancellationToken) =>
        isAdmin
            ? Task.FromResult(true)
            : db.ProjectMembers.AnyAsync(
                x => x.ProjectId == projectId && x.UserId == userId && (x.Role == ProjectRole.Manager || x.Role == ProjectRole.Owner),
                cancellationToken);
}

