using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Quraaa.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLibraryMagicLinkEmailVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ConcurrencyStamp",
                table: "Libraries",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailVerifiedAtUtc",
                table: "Libraries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "Libraries"
                SET "ApprovalStatus" = 4
                WHERE "ApprovalStatus" = 1;

                UPDATE "Libraries"
                SET "EmailVerifiedAtUtc" = CURRENT_TIMESTAMP
                WHERE "ApprovalStatus" IN (2, 3)
                  AND "EmailVerifiedAtUtc" IS NULL;
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Libraries_ApprovalStatus_EmailVerification",
                table: "Libraries",
                sql: "\"ApprovalStatus\" = 4 OR (\"ApprovalStatus\" IN (1, 2, 3) AND \"EmailVerifiedAtUtc\" IS NOT NULL)");

            migrationBuilder.CreateTable(
                name: "LibraryEmailVerificationChallenges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LibraryId = table.Column<Guid>(type: "uuid", nullable: false),
                    CodeHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Generation = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResendAvailableAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FailedAttemptCount = table.Column<int>(type: "integer", nullable: false),
                    LockedUntilUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConsumedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SendWindowStartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SendCount = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_LibraryEmailVerificationChallenges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LibraryEmailVerificationChallenges_Libraries_LibraryId",
                        column: x => x.LibraryId,
                        principalTable: "Libraries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LibraryRegistrationSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthenticationSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SubmittedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_LibraryRegistrationSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LibraryRegistrationSessions_UsersProfiles_UserId",
                        column: x => x.UserId,
                        principalTable: "UsersProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LibraryEmailVerificationChallenges_LibraryId",
                table: "LibraryEmailVerificationChallenges",
                column: "LibraryId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LibraryRegistrationSessions_TokenHash",
                table: "LibraryRegistrationSessions",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LibraryRegistrationSessions_UserId",
                table: "LibraryRegistrationSessions",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LibraryEmailVerificationChallenges");

            migrationBuilder.DropTable(
                name: "LibraryRegistrationSessions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Libraries_ApprovalStatus_EmailVerification",
                table: "Libraries");

            migrationBuilder.Sql(
                """
                UPDATE "Libraries"
                SET "ApprovalStatus" = 1
                WHERE "ApprovalStatus" = 4;
                """);

            migrationBuilder.DropColumn(
                name: "ConcurrencyStamp",
                table: "Libraries");

            migrationBuilder.DropColumn(
                name: "EmailVerifiedAtUtc",
                table: "Libraries");
        }
    }
}
