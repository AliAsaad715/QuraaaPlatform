using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Quraaa.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixRelationBetwenInterestsAndCategroies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Interest_UsersProfiles_UserId",
                table: "Interest");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Interest",
                table: "Interest");

            migrationBuilder.DropIndex(
                name: "IX_Interest_UserId",
                table: "Interest");

            migrationBuilder.RenameTable(
                name: "Interest",
                newName: "UserInterests");

            migrationBuilder.AddColumn<Guid>(
                name: "UserAggregateId",
                table: "UserInterests",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserInterests",
                table: "UserInterests",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_UserInterests_CategoryId",
                table: "UserInterests",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_UserInterests_UserAggregateId",
                table: "UserInterests",
                column: "UserAggregateId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserInterests_Categories_CategoryId",
                table: "UserInterests",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserInterests_UsersProfiles_UserAggregateId",
                table: "UserInterests",
                column: "UserAggregateId",
                principalTable: "UsersProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserInterests_Categories_CategoryId",
                table: "UserInterests");

            migrationBuilder.DropForeignKey(
                name: "FK_UserInterests_UsersProfiles_UserAggregateId",
                table: "UserInterests");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserInterests",
                table: "UserInterests");

            migrationBuilder.DropIndex(
                name: "IX_UserInterests_CategoryId",
                table: "UserInterests");

            migrationBuilder.DropIndex(
                name: "IX_UserInterests_UserAggregateId",
                table: "UserInterests");

            migrationBuilder.DropColumn(
                name: "UserAggregateId",
                table: "UserInterests");

            migrationBuilder.RenameTable(
                name: "UserInterests",
                newName: "Interest");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Interest",
                table: "Interest",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Interest_UserId",
                table: "Interest",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Interest_UsersProfiles_UserId",
                table: "Interest",
                column: "UserId",
                principalTable: "UsersProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
