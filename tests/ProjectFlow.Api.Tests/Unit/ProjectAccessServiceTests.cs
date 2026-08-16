using Microsoft.EntityFrameworkCore;
using ProjectFlow.Api.Data;
using ProjectFlow.Api.Domain;
using ProjectFlow.Api.Infrastructure;

namespace ProjectFlow.Api.Tests.Unit;

public sealed class ProjectAccessServiceTests
{
    [Fact]
    public async Task Viewer_CanRead_ButCannotContribute()
    {
        await using var db = CreateDb();
        var projectId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();
        db.ProjectMembers.Add(new ProjectMember { ProjectId = projectId, UserId = userId, Role = ProjectRole.Viewer });
        await db.SaveChangesAsync();
        var service = new ProjectAccessService(db);

        Assert.True(await service.CanReadAsync(projectId, userId, false, CancellationToken.None));
        Assert.False(await service.CanContributeAsync(projectId, userId, false, CancellationToken.None));
    }

    [Fact]
    public async Task Admin_CanManage_WithoutMembership()
    {
        await using var db = CreateDb();
        var service = new ProjectAccessService(db);

        Assert.True(await service.CanManageAsync(Guid.CreateVersion7(), Guid.CreateVersion7(), true, CancellationToken.None));
    }

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }
}

