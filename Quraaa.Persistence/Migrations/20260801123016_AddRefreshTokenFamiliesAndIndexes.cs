using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Quraaa.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRefreshTokenFamiliesAndIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RefreshTokenFamilyId",
                table: "AspNetUsers",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ConsumedRefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    FamilyId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ConsumedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsumedRefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsumedRefreshTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_RefreshToken",
                table: "AspNetUsers",
                column: "RefreshToken",
                unique: true,
                filter: "\"RefreshToken\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_RefreshTokenFamilyId",
                table: "AspNetUsers",
                column: "RefreshTokenFamilyId",
                unique: true,
                filter: "\"RefreshTokenFamilyId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ConsumedRefreshTokens_ExpiresAtUtc",
                table: "ConsumedRefreshTokens",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ConsumedRefreshTokens_TokenHash",
                table: "ConsumedRefreshTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConsumedRefreshTokens_UserId_FamilyId",
                table: "ConsumedRefreshTokens",
                columns: new[] { "UserId", "FamilyId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConsumedRefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_RefreshToken",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_RefreshTokenFamilyId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "RefreshTokenFamilyId",
                table: "AspNetUsers");
        }
    }
}
