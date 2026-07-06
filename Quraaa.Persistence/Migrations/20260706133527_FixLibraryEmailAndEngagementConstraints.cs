using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Quraaa.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixLibraryEmailAndEngagementConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE \"Libraries\" SET \"Email\" = lower(trim(\"Email\"));");

            migrationBuilder.DropIndex(
                name: "IX_Libraries_Email",
                table: "Libraries");

            migrationBuilder.DropIndex(
                name: "IX_FavoriteBooks_UserId_BookId",
                table: "FavoriteBooks");

            migrationBuilder.CreateIndex(
                name: "IX_Libraries_Email",
                table: "Libraries",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FavoriteBooks_UserId_BookId",
                table: "FavoriteBooks",
                columns: new[] { "UserId", "BookId" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.AddForeignKey(
                name: "FK_BookPurchases_Books_BookId",
                table: "BookPurchases",
                column: "BookId",
                principalTable: "Books",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BookPurchases_Listings_ListingId",
                table: "BookPurchases",
                column: "ListingId",
                principalTable: "Listings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BookPurchases_UsersProfiles_UserId",
                table: "BookPurchases",
                column: "UserId",
                principalTable: "UsersProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BookRatings_Books_BookId",
                table: "BookRatings",
                column: "BookId",
                principalTable: "Books",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BookRatings_UsersProfiles_UserId",
                table: "BookRatings",
                column: "UserId",
                principalTable: "UsersProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FavoriteBooks_Books_BookId",
                table: "FavoriteBooks",
                column: "BookId",
                principalTable: "Books",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FavoriteBooks_UsersProfiles_UserId",
                table: "FavoriteBooks",
                column: "UserId",
                principalTable: "UsersProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BookPurchases_Books_BookId",
                table: "BookPurchases");

            migrationBuilder.DropForeignKey(
                name: "FK_BookPurchases_Listings_ListingId",
                table: "BookPurchases");

            migrationBuilder.DropForeignKey(
                name: "FK_BookPurchases_UsersProfiles_UserId",
                table: "BookPurchases");

            migrationBuilder.DropForeignKey(
                name: "FK_BookRatings_Books_BookId",
                table: "BookRatings");

            migrationBuilder.DropForeignKey(
                name: "FK_BookRatings_UsersProfiles_UserId",
                table: "BookRatings");

            migrationBuilder.DropForeignKey(
                name: "FK_FavoriteBooks_Books_BookId",
                table: "FavoriteBooks");

            migrationBuilder.DropForeignKey(
                name: "FK_FavoriteBooks_UsersProfiles_UserId",
                table: "FavoriteBooks");

            migrationBuilder.DropIndex(
                name: "IX_Libraries_Email",
                table: "Libraries");

            migrationBuilder.DropIndex(
                name: "IX_FavoriteBooks_UserId_BookId",
                table: "FavoriteBooks");

            migrationBuilder.CreateIndex(
                name: "IX_Libraries_Email",
                table: "Libraries",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_FavoriteBooks_UserId_BookId",
                table: "FavoriteBooks",
                columns: new[] { "UserId", "BookId" },
                unique: true);
        }
    }
}
