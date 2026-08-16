using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectFlow.Api.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("202608160001_InitialCreate")]
public sealed class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Users",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                Email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                PasswordHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                Role = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Users", x => x.Id));

        migrationBuilder.CreateTable(
            name: "Projects",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                Key = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                CreatedById = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Projects", x => x.Id);
                table.ForeignKey("FK_Projects_Users_CreatedById", x => x.CreatedById, "Users", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "ProjectMembers",
            columns: table => new
            {
                ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                Role = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                JoinedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProjectMembers", x => new { x.ProjectId, x.UserId });
                table.ForeignKey("FK_ProjectMembers_Projects_ProjectId", x => x.ProjectId, "Projects", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_ProjectMembers_Users_UserId", x => x.UserId, "Users", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "WorkItems",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Description = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                Priority = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                AssigneeId = table.Column<Guid>(type: "uuid", nullable: true),
                CreatedById = table.Column<Guid>(type: "uuid", nullable: false),
                DueAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_WorkItems", x => x.Id);
                table.ForeignKey("FK_WorkItems_Projects_ProjectId", x => x.ProjectId, "Projects", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_WorkItems_Users_AssigneeId", x => x.AssigneeId, "Users", "Id", onDelete: ReferentialAction.SetNull);
                table.ForeignKey("FK_WorkItems_Users_CreatedById", x => x.CreatedById, "Users", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Comments",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                WorkItemId = table.Column<Guid>(type: "uuid", nullable: false),
                AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                Body = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Comments", x => x.Id);
                table.ForeignKey("FK_Comments_Users_AuthorId", x => x.AuthorId, "Users", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_Comments_WorkItems_WorkItemId", x => x.WorkItemId, "WorkItems", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Attachments",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                WorkItemId = table.Column<Guid>(type: "uuid", nullable: false),
                UploadedById = table.Column<Guid>(type: "uuid", nullable: false),
                OriginalName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                StoredName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                ContentType = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Attachments", x => x.Id);
                table.ForeignKey("FK_Attachments_Users_UploadedById", x => x.UploadedById, "Users", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_Attachments_WorkItems_WorkItemId", x => x.WorkItemId, "WorkItems", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_Users_Email", "Users", "Email", unique: true);
        migrationBuilder.CreateIndex("IX_Projects_CreatedById", "Projects", "CreatedById");
        migrationBuilder.CreateIndex("IX_Projects_Key", "Projects", "Key", unique: true);
        migrationBuilder.CreateIndex("IX_ProjectMembers_UserId", "ProjectMembers", "UserId");
        migrationBuilder.CreateIndex("IX_WorkItems_AssigneeId", "WorkItems", "AssigneeId");
        migrationBuilder.CreateIndex("IX_WorkItems_CreatedById", "WorkItems", "CreatedById");
        migrationBuilder.CreateIndex("IX_WorkItems_ProjectId_Status", "WorkItems", new[] { "ProjectId", "Status" });
        migrationBuilder.CreateIndex("IX_Comments_AuthorId", "Comments", "AuthorId");
        migrationBuilder.CreateIndex("IX_Comments_WorkItemId", "Comments", "WorkItemId");
        migrationBuilder.CreateIndex("IX_Attachments_UploadedById", "Attachments", "UploadedById");
        migrationBuilder.CreateIndex("IX_Attachments_WorkItemId", "Attachments", "WorkItemId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("Attachments");
        migrationBuilder.DropTable("Comments");
        migrationBuilder.DropTable("ProjectMembers");
        migrationBuilder.DropTable("WorkItems");
        migrationBuilder.DropTable("Projects");
        migrationBuilder.DropTable("Users");
    }
}

