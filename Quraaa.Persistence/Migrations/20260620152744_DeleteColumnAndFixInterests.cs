using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Quraaa.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DeleteColumnAndFixInterests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserInterests_UsersProfiles_UserAggregateId",
                table: "UserInterests");

            migrationBuilder.DropIndex(
                name: "IX_UserInterests_UserAggregateId",
                table: "UserInterests");

            migrationBuilder.DropColumn(
                name: "UserAggregateId",
                table: "UserInterests");

            migrationBuilder.CreateIndex(
                name: "IX_UserInterests_UserId_CategoryId",
                table: "UserInterests",
                columns: new[] { "UserId", "CategoryId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_UserInterests_UsersProfiles_UserId",
                table: "UserInterests",
                column: "UserId",
                principalTable: "UsersProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserInterests_UsersProfiles_UserId",
                table: "UserInterests");

            migrationBuilder.DropIndex(
                name: "IX_UserInterests_UserId_CategoryId",
                table: "UserInterests");

            migrationBuilder.AddColumn<Guid>(
                name: "UserAggregateId",
                table: "UserInterests",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_UserInterests_UserAggregateId",
                table: "UserInterests",
                column: "UserAggregateId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserInterests_UsersProfiles_UserAggregateId",
                table: "UserInterests",
                column: "UserAggregateId",
                principalTable: "UsersProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
