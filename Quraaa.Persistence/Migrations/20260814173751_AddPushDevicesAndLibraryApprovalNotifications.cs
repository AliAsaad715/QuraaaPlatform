using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Quraaa.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPushDevicesAndLibraryApprovalNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LibraryApprovalNotifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LibraryId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipientEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    LibraryName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EmailState = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    PushState = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    EmailAttemptCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    PushAttemptCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    EmailNextAttemptAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PushNextAttemptAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EmailCompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_LibraryApprovalNotifications", x => x.Id);
                    table.CheckConstraint("CK_LibraryApprovalNotifications_AttemptCounts_NonNegative", "\"EmailAttemptCount\" >= 0 AND \"PushAttemptCount\" >= 0");
                    table.CheckConstraint("CK_LibraryApprovalNotifications_EmailState_Valid", "\"EmailState\" BETWEEN 1 AND 4");
                    table.CheckConstraint("CK_LibraryApprovalNotifications_PushState_Valid", "\"PushState\" BETWEEN 1 AND 4");
                    table.ForeignKey(
                        name: "FK_LibraryApprovalNotifications_Libraries_LibraryId",
                        column: x => x.LibraryId,
                        principalTable: "Libraries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LibraryApprovalNotifications_UsersProfiles_UserId",
                        column: x => x.UserId,
                        principalTable: "UsersProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PushDevices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PushDevices", x => x.Id);
                    table.CheckConstraint("CK_PushDevices_Token_NotBlank", "btrim(\"Token\") <> ''");
                    table.CheckConstraint("CK_PushDevices_Token_NoWhitespace", "\"Token\" !~ '[[:space:]]'");
                    table.CheckConstraint("CK_PushDevices_TokenHash_Valid", "char_length(\"TokenHash\") = 64 AND \"TokenHash\" ~ '^[0-9A-F]{64}$'");
                    table.ForeignKey(
                        name: "FK_PushDevices_UsersProfiles_UserId",
                        column: x => x.UserId,
                        principalTable: "UsersProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LibraryApprovalNotifications_EmailState_EmailNextAttemptAtU~",
                table: "LibraryApprovalNotifications",
                columns: new[] { "EmailState", "EmailNextAttemptAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_LibraryApprovalNotifications_LeaseUntilUtc",
                table: "LibraryApprovalNotifications",
                column: "LeaseUntilUtc");

            migrationBuilder.CreateIndex(
                name: "IX_LibraryApprovalNotifications_LibraryId",
                table: "LibraryApprovalNotifications",
                column: "LibraryId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LibraryApprovalNotifications_PushState_PushNextAttemptAtUtc",
                table: "LibraryApprovalNotifications",
                columns: new[] { "PushState", "PushNextAttemptAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_LibraryApprovalNotifications_UserId",
                table: "LibraryApprovalNotifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PushDevices_TokenHash",
                table: "PushDevices",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PushDevices_UserId",
                table: "PushDevices",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LibraryApprovalNotifications");

            migrationBuilder.DropTable(
                name: "PushDevices");
        }
    }
}
