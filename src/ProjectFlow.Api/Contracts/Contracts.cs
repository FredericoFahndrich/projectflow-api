using System.ComponentModel.DataAnnotations;
using ProjectFlow.Api.Domain;

namespace ProjectFlow.Api.Contracts;

public sealed record RegisterRequest(
    [Required, MaxLength(120)] string Name,
    [Required, EmailAddress, MaxLength(254)] string Email,
    [Required, MinLength(10), MaxLength(128)] string Password);

public sealed record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);

public sealed record AuthResponse(string AccessToken, DateTimeOffset ExpiresAt, UserResponse User);

public sealed record UserResponse(Guid Id, string Name, string Email, UserRole Role, DateTimeOffset CreatedAt);

public sealed record UpdateUserRoleRequest(UserRole Role);

public sealed record CreateProjectRequest(
    [Required, MaxLength(160)] string Name,
    [Required, RegularExpression("^[A-Za-z][A-Za-z0-9]{2,9}$")] string Key,
    [MaxLength(2000)] string? Description);

public sealed record UpdateProjectRequest(
    [Required, MaxLength(160)] string Name,
    [MaxLength(2000)] string? Description);

public sealed record AddProjectMemberRequest(
    [Required, EmailAddress] string Email,
    ProjectRole Role);

public sealed record ProjectMemberResponse(Guid UserId, string Name, string Email, ProjectRole Role, DateTimeOffset JoinedAt);

public sealed record ProjectResponse(
    Guid Id,
    string Name,
    string Key,
    string? Description,
    Guid CreatedById,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyCollection<ProjectMemberResponse>? Members = null);

public sealed record CreateWorkItemRequest(
    [Required, MaxLength(200)] string Title,
    [MaxLength(5000)] string? Description,
    WorkItemPriority Priority = WorkItemPriority.Medium,
    Guid? AssigneeId = null,
    DateTimeOffset? DueAt = null);

public sealed record UpdateWorkItemRequest(
    [Required, MaxLength(200)] string Title,
    [MaxLength(5000)] string? Description,
    WorkItemStatus Status,
    WorkItemPriority Priority,
    Guid? AssigneeId,
    DateTimeOffset? DueAt);

public sealed record WorkItemResponse(
    Guid Id,
    Guid ProjectId,
    string Title,
    string? Description,
    WorkItemStatus Status,
    WorkItemPriority Priority,
    Guid? AssigneeId,
    Guid CreatedById,
    DateTimeOffset? DueAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CreateCommentRequest([Required, MaxLength(4000)] string Body);

public sealed record CommentResponse(Guid Id, Guid WorkItemId, Guid AuthorId, string AuthorName, string Body, DateTimeOffset CreatedAt);

public sealed record AttachmentResponse(
    Guid Id,
    Guid WorkItemId,
    Guid UploadedById,
    string OriginalName,
    string ContentType,
    long SizeBytes,
    DateTimeOffset CreatedAt,
    string DownloadUrl);

public static class ContractMappings
{
    public static UserResponse ToResponse(this User user) =>
        new(user.Id, user.Name, user.Email, user.Role, user.CreatedAt);

    public static ProjectResponse ToResponse(this Project project, bool includeMembers = false) =>
        new(
            project.Id,
            project.Name,
            project.Key,
            project.Description,
            project.CreatedById,
            project.CreatedAt,
            project.UpdatedAt,
            includeMembers
                ? project.Members.Select(x => new ProjectMemberResponse(x.UserId, x.User.Name, x.User.Email, x.Role, x.JoinedAt)).ToArray()
                : null);

    public static WorkItemResponse ToResponse(this WorkItem item) =>
        new(item.Id, item.ProjectId, item.Title, item.Description, item.Status, item.Priority, item.AssigneeId, item.CreatedById, item.DueAt, item.CreatedAt, item.UpdatedAt);

    public static CommentResponse ToResponse(this Comment comment) =>
        new(comment.Id, comment.WorkItemId, comment.AuthorId, comment.Author.Name, comment.Body, comment.CreatedAt);

    public static AttachmentResponse ToResponse(this Attachment attachment) =>
        new(attachment.Id, attachment.WorkItemId, attachment.UploadedById, attachment.OriginalName, attachment.ContentType, attachment.SizeBytes, attachment.CreatedAt, $"/api/attachments/{attachment.Id}/download");
}

