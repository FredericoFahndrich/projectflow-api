namespace ProjectFlow.Api.Domain;

public enum UserRole
{
    Member,
    Admin
}

public enum ProjectRole
{
    Viewer,
    Contributor,
    Manager,
    Owner
}

public enum WorkItemStatus
{
    Backlog,
    Todo,
    InProgress,
    Blocked,
    Done
}

public enum WorkItemPriority
{
    Low,
    Medium,
    High,
    Critical
}

public sealed class User
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public UserRole Role { get; set; } = UserRole.Member;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<ProjectMember> ProjectMemberships { get; set; } = [];
}

public sealed class Project
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public required string Name { get; set; }
    public required string Key { get; set; }
    public string? Description { get; set; }
    public Guid CreatedById { get; set; }
    public User CreatedBy { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<ProjectMember> Members { get; set; } = [];
    public ICollection<WorkItem> WorkItems { get; set; } = [];
}

public sealed class ProjectMember
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public ProjectRole Role { get; set; }
    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class WorkItem
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    public required string Title { get; set; }
    public string? Description { get; set; }
    public WorkItemStatus Status { get; set; } = WorkItemStatus.Backlog;
    public WorkItemPriority Priority { get; set; } = WorkItemPriority.Medium;
    public Guid? AssigneeId { get; set; }
    public User? Assignee { get; set; }
    public Guid CreatedById { get; set; }
    public User CreatedBy { get; set; } = null!;
    public DateTimeOffset? DueAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<Comment> Comments { get; set; } = [];
    public ICollection<Attachment> Attachments { get; set; } = [];
}

public sealed class Comment
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid WorkItemId { get; set; }
    public WorkItem WorkItem { get; set; } = null!;
    public Guid AuthorId { get; set; }
    public User Author { get; set; } = null!;
    public required string Body { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class Attachment
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid WorkItemId { get; set; }
    public WorkItem WorkItem { get; set; } = null!;
    public Guid UploadedById { get; set; }
    public User UploadedBy { get; set; } = null!;
    public required string OriginalName { get; set; }
    public required string StoredName { get; set; }
    public required string ContentType { get; set; }
    public long SizeBytes { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

