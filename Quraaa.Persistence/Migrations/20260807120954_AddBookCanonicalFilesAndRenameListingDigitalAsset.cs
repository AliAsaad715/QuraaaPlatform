using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Quraaa.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBookCanonicalFilesAndRenameListingDigitalAsset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Listings_DigitalAssetUrl",
                table: "Listings");

            migrationBuilder.RenameColumn(
                name: "DigitalAssetUrl",
                table: "Listings",
                newName: "CustomDigitalAssetUrl");

            migrationBuilder.AddColumn<string>(
                name: "CanonicalPdfUrl",
                table: "Books",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CanonicalWordDocUrl",
                table: "Books",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Listings_CustomDigitalAssetUrl",
                table: "Listings",
                column: "CustomDigitalAssetUrl",
                filter: "\"CustomDigitalAssetUrl\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Listings_CustomDigitalAssetUrl",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "CanonicalPdfUrl",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "CanonicalWordDocUrl",
                table: "Books");

            migrationBuilder.RenameColumn(
                name: "CustomDigitalAssetUrl",
                table: "Listings",
                newName: "DigitalAssetUrl");

            migrationBuilder.CreateIndex(
                name: "IX_Listings_DigitalAssetUrl",
                table: "Listings",
                column: "DigitalAssetUrl",
                filter: "\"DigitalAssetUrl\" IS NOT NULL");
        }
    }
}
