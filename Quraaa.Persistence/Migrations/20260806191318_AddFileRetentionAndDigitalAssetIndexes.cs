using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Quraaa.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFileRetentionAndDigitalAssetIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Books_Isbn",
                table: "Books");

            migrationBuilder.DropIndex(
                name: "IX_Books_Title_Author_Language",
                table: "Books");

            // Replaces the raw-value unique index dropped above with a functional index on
            // lower() expressions, matching BookTextNormalizer's case-insensitive comparison.
            // EF Core's fluent API cannot express expression indexes, so this is raw SQL —
            // deliberately absent from BookConfiguration; see its comments for why.
            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX ""IX_Books_Title_Author_Language_CI""
                ON ""Books"" (lower(""Title""), lower(""Author""), lower(""Language""));
            ");

            migrationBuilder.AddColumn<string>(
                name: "PurchasedDigitalAssetUrl",
                table: "BookPurchases",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OrphanFileCandidates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RelativePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DetectedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrphanFileCandidates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Listings_DigitalAssetUrl",
                table: "Listings",
                column: "DigitalAssetUrl",
                filter: "\"DigitalAssetUrl\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Books_Isbn",
                table: "Books",
                column: "Isbn",
                unique: true,
                filter: "\"Isbn\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BookPurchases_PurchasedDigitalAssetUrl",
                table: "BookPurchases",
                column: "PurchasedDigitalAssetUrl",
                filter: "\"PurchasedDigitalAssetUrl\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OrphanFileCandidates_RelativePath",
                table: "OrphanFileCandidates",
                column: "RelativePath",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrphanFileCandidates_Status_DetectedAtUtc",
                table: "OrphanFileCandidates",
                columns: new[] { "Status", "DetectedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Books_Title_Author_Language_CI"";");

            migrationBuilder.DropTable(
                name: "OrphanFileCandidates");

            migrationBuilder.DropIndex(
                name: "IX_Listings_DigitalAssetUrl",
                table: "Listings");

            migrationBuilder.DropIndex(
                name: "IX_Books_Isbn",
                table: "Books");

            migrationBuilder.DropIndex(
                name: "IX_BookPurchases_PurchasedDigitalAssetUrl",
                table: "BookPurchases");

            migrationBuilder.DropColumn(
                name: "PurchasedDigitalAssetUrl",
                table: "BookPurchases");

            migrationBuilder.CreateIndex(
                name: "IX_Books_Isbn",
                table: "Books",
                column: "Isbn",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Books_Title_Author_Language",
                table: "Books",
                columns: new[] { "Title", "Author", "Language" },
                unique: true);
        }
    }
}
