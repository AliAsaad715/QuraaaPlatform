using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Quraaa.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddListingPushNotificationOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ListingPushNotifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LibraryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    BookId = table.Column<Guid>(type: "uuid", nullable: true),
                    ListingId = table.Column<Guid>(type: "uuid", nullable: true),
                    ItemCount = table.Column<int>(type: "integer", nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    NextAttemptAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_ListingPushNotifications", x => x.Id);
                    table.CheckConstraint("CK_ListingPushNotifications_AttemptCount_NonNegative", "\"AttemptCount\" >= 0");
                    table.CheckConstraint("CK_ListingPushNotifications_ItemCount_Positive", "\"ItemCount\" > 0");
                    table.CheckConstraint("CK_ListingPushNotifications_Payload_Valid", "(\"Type\" = 1 AND ((\"ItemCount\" = 1 AND \"BookId\" IS NOT NULL AND \"ListingId\" IS NOT NULL) OR (\"ItemCount\" > 1 AND \"BookId\" IS NULL AND \"ListingId\" IS NULL))) OR (\"Type\" = 2 AND \"ItemCount\" = 1 AND \"BookId\" IS NOT NULL AND \"ListingId\" IS NOT NULL)");
                    table.CheckConstraint("CK_ListingPushNotifications_State_Valid", "\"State\" BETWEEN 1 AND 4");
                    table.CheckConstraint("CK_ListingPushNotifications_Type_Valid", "\"Type\" BETWEEN 1 AND 2");
                    table.ForeignKey(
                        name: "FK_ListingPushNotifications_Books_BookId",
                        column: x => x.BookId,
                        principalTable: "Books",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ListingPushNotifications_Libraries_LibraryId",
                        column: x => x.LibraryId,
                        principalTable: "Libraries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ListingPushNotifications_Listings_ListingId",
                        column: x => x.ListingId,
                        principalTable: "Listings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ListingPushNotifications_BookId",
                table: "ListingPushNotifications",
                column: "BookId");

            migrationBuilder.CreateIndex(
                name: "IX_ListingPushNotifications_LeaseUntilUtc",
                table: "ListingPushNotifications",
                column: "LeaseUntilUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ListingPushNotifications_LibraryId",
                table: "ListingPushNotifications",
                column: "LibraryId");

            migrationBuilder.CreateIndex(
                name: "IX_ListingPushNotifications_ListingId",
                table: "ListingPushNotifications",
                column: "ListingId");

            migrationBuilder.CreateIndex(
                name: "IX_ListingPushNotifications_State_NextAttemptAtUtc",
                table: "ListingPushNotifications",
                columns: new[] { "State", "NextAttemptAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ListingPushNotifications");
        }
    }
}
