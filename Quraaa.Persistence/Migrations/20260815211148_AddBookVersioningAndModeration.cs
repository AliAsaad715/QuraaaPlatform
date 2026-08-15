using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Quraaa.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBookVersioningAndModeration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrentVersionNumber",
                table: "Books",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateTime>(
                name: "HiddenAtUtc",
                table: "Books",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModerationNote",
                table: "Books",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ModerationStatus",
                table: "Books",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "BookModerationNotifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BookId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipientUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Audience = table.Column<int>(type: "integer", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    ReporterCount = table.Column<int>(type: "integer", nullable: false),
                    BookTitle = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PushState = table.Column<int>(type: "integer", nullable: false),
                    PushAttemptCount = table.Column<int>(type: "integer", nullable: false),
                    PushNextAttemptAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PushCompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeliveredPushTokenHashes = table.Column<string[]>(type: "text[]", nullable: false),
                    LeaseUntilUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeleationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookModerationNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookModerationNotifications_Books_BookId",
                        column: x => x.BookId,
                        principalTable: "Books",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BookModerationNotifications_UsersProfiles_RecipientUserId",
                        column: x => x.RecipientUserId,
                        principalTable: "UsersProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BookVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BookId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<int>(type: "integer", nullable: false),
                    RevertedFromVersionNumber = table.Column<int>(type: "integer", nullable: true),
                    ChangedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: false),
                    CoverImageUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    Language = table.Column<int>(type: "integer", nullable: false),
                    Isbn = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeleationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookVersions", x => x.Id);
                    table.CheckConstraint("CK_BookVersions_RevertedFrom_Consistent", "(\"Reason\" = 3 AND \"RevertedFromVersionNumber\" IS NOT NULL) OR (\"Reason\" <> 3 AND \"RevertedFromVersionNumber\" IS NULL)");
                    table.CheckConstraint("CK_BookVersions_VersionNumber_Positive", "\"VersionNumber\" > 0");
                    table.ForeignKey(
                        name: "FK_BookVersions_Books_BookId",
                        column: x => x.BookId,
                        principalTable: "Books",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Books_ModerationStatus",
                table: "Books",
                column: "ModerationStatus");

            migrationBuilder.CreateIndex(
                name: "IX_BookModerationNotifications_BookId",
                table: "BookModerationNotifications",
                column: "BookId");

            migrationBuilder.CreateIndex(
                name: "IX_BookModerationNotifications_PushState_PushNextAttemptAtUtc",
                table: "BookModerationNotifications",
                columns: new[] { "PushState", "PushNextAttemptAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_BookModerationNotifications_RecipientUserId",
                table: "BookModerationNotifications",
                column: "RecipientUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BookVersions_BookId_VersionNumber",
                table: "BookVersions",
                columns: new[] { "BookId", "VersionNumber" },
                unique: true);

            // Every existing book gets the version 1 that the interceptor writes
            // for new ones, so history and revert work for the whole catalogue
            // and not just for books created from here on.
            migrationBuilder.Sql(@"
                INSERT INTO ""BookVersions"" (
                    ""Id"", ""BookId"", ""VersionNumber"", ""Reason"",
                    ""RevertedFromVersionNumber"", ""ChangedByUserId"",
                    ""Title"", ""AuthorId"", ""Description"", ""CoverImageUrl"",
                    ""CategoryId"", ""Language"", ""Isbn"",
                    ""CreationTime"", ""IsDeleted"")
                SELECT
                    gen_random_uuid(), b.""Id"", 1, 1,
                    NULL, NULL,
                    LEFT(b.""Title"", 500), b.""AuthorId"", b.""Description"",
                    LEFT(b.""CoverImageUrl"", 1000),
                    b.""CategoryId"", b.""Language"", LEFT(b.""Isbn"", 20),
                    b.""CreationTime"", FALSE
                FROM ""Books"" AS b
                WHERE NOT EXISTS (
                    SELECT 1 FROM ""BookVersions"" AS v WHERE v.""BookId"" = b.""Id"");
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookModerationNotifications");

            migrationBuilder.DropTable(
                name: "BookVersions");

            migrationBuilder.DropIndex(
                name: "IX_Books_ModerationStatus",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "CurrentVersionNumber",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "HiddenAtUtc",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "ModerationNote",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "ModerationStatus",
                table: "Books");
        }
    }
}
