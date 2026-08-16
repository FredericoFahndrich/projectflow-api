using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ProjectFlow.Api.Contracts;
using ProjectFlow.Api.Domain;

namespace ProjectFlow.Api.Tests.Integration;

public sealed class ProjectWorkflowTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task AuthenticatedUser_CanCreateProjectWorkItemAndComment()
    {
        var client = factory.CreateClient();
        var email = $"user-{Guid.NewGuid():N}@example.com";
        var register = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("Grace Hopper", email, "StrongPass123!"));
        register.EnsureSuccessStatusCode();
        var auth = await register.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        var createProject = await client.PostAsJsonAsync("/api/projects", new CreateProjectRequest("Compiler Platform", "CMP", "Delivery board"));
        Assert.Equal(HttpStatusCode.Created, createProject.StatusCode);
        var project = await createProject.Content.ReadFromJsonAsync<ProjectResponse>();
        Assert.NotNull(project);

        var createItem = await client.PostAsJsonAsync(
            $"/api/projects/{project.Id}/work-items",
            new CreateWorkItemRequest("Create parser", "Implement the first parsing pass", WorkItemPriority.High));
        Assert.Equal(HttpStatusCode.Created, createItem.StatusCode);
        var item = await createItem.Content.ReadFromJsonAsync<WorkItemResponse>();
        Assert.NotNull(item);

        var createComment = await client.PostAsJsonAsync(
            $"/api/work-items/{item.Id}/comments",
            new CreateCommentRequest("The grammar draft is ready for review."));
        Assert.Equal(HttpStatusCode.Created, createComment.StatusCode);

        var list = await client.GetFromJsonAsync<WorkItemResponse[]>($"/api/projects/{project.Id}/work-items");
        Assert.Single(list!);
        Assert.Equal("Create parser", list![0].Title);
    }

    [Fact]
    public async Task AnonymousUser_CannotListProjects()
    {
        var response = await factory.CreateClient().GetAsync("/api/projects");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

