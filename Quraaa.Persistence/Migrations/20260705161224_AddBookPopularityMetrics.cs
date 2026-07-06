using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Quraaa.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBookPopularityMetrics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BookPurchases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    BookId = table.Column<Guid>(type: "uuid", nullable: false),
                    ListingId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeleationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookPurchases", x => x.Id);
                    table.CheckConstraint("CK_BookPurchases_Quantity_Positive", "\"Quantity\" > 0");
                    table.CheckConstraint("CK_BookPurchases_UnitPrice_NonNegative", "\"UnitPrice\" >= 0");
                });

            migrationBuilder.CreateTable(
                name: "BookRatings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    BookId = table.Column<Guid>(type: "uuid", nullable: false),
                    RatingValue = table.Column<int>(type: "integer", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeleationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookRatings", x => x.Id);
                    table.CheckConstraint("CK_BookRatings_RatingValue_Range", "\"RatingValue\" >= 1 AND \"RatingValue\" <= 5");
                });

            migrationBuilder.CreateIndex(
                name: "IX_BookPurchases_BookId",
                table: "BookPurchases",
                column: "BookId");

            migrationBuilder.CreateIndex(
                name: "IX_BookPurchases_BookId_CreationTime",
                table: "BookPurchases",
                columns: new[] { "BookId", "CreationTime" });

            migrationBuilder.CreateIndex(
                name: "IX_BookPurchases_CreationTime",
                table: "BookPurchases",
                column: "CreationTime");

            migrationBuilder.CreateIndex(
                name: "IX_BookPurchases_ListingId",
                table: "BookPurchases",
                column: "ListingId");

            migrationBuilder.CreateIndex(
                name: "IX_BookPurchases_UserId",
                table: "BookPurchases",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_BookRatings_BookId",
                table: "BookRatings",
                column: "BookId");

            migrationBuilder.CreateIndex(
                name: "IX_BookRatings_CreationTime",
                table: "BookRatings",
                column: "CreationTime");

            migrationBuilder.CreateIndex(
                name: "IX_BookRatings_RatingValue",
                table: "BookRatings",
                column: "RatingValue");

            migrationBuilder.CreateIndex(
                name: "IX_BookRatings_UserId",
                table: "BookRatings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_BookRatings_UserId_BookId",
                table: "BookRatings",
                columns: new[] { "UserId", "BookId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookPurchases");

            migrationBuilder.DropTable(
                name: "BookRatings");
        }
    }
}
