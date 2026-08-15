using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Quraaa.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLibraryStripeWalletAndSellerPayouts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StripeConnectAccountId",
                table: "Libraries",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StripeWalletActivatedAtUtc",
                table: "Libraries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ProfitSharePercent",
                table: "Libraries",
                type: "decimal(7,4)",
                nullable: false,
                defaultValue: 0.005m);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Libraries_ProfitSharePercent_Range",
                table: "Libraries",
                sql: "\"ProfitSharePercent\" >= 0 AND \"ProfitSharePercent\" <= 100");

            migrationBuilder.CreateTable(
                name: "SellerPayouts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    LibraryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    GrossAmountMinor = table.Column<long>(type: "bigint", nullable: false),
                    ProfitSharePercent = table.Column<decimal>(type: "decimal(7,4)", nullable: false),
                    PlatformFeeMinor = table.Column<long>(type: "bigint", nullable: false),
                    NetAmountMinor = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    IdempotencyKeyGeneration = table.Column<int>(type: "integer", nullable: false),
                    NextAttemptAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastAttemptAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PaidAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StripeTransferId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    DestinationStripeAccountId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SourcePaymentIntentId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    SourceChargeId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeleationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SellerPayouts", x => x.Id);
                    table.CheckConstraint("CK_SellerPayouts_Amounts_Consistent", "\"GrossAmountMinor\" = \"NetAmountMinor\" + \"PlatformFeeMinor\"");
                    table.CheckConstraint("CK_SellerPayouts_GrossAmountMinor_Positive", "\"GrossAmountMinor\" > 0");
                    table.CheckConstraint("CK_SellerPayouts_NetAmountMinor_NonNegative", "\"NetAmountMinor\" >= 0");
                    table.CheckConstraint("CK_SellerPayouts_PlatformFeeMinor_NonNegative", "\"PlatformFeeMinor\" >= 0");
                    table.CheckConstraint("CK_SellerPayouts_ProfitSharePercent_Range", "\"ProfitSharePercent\" >= 0 AND \"ProfitSharePercent\" <= 100");
                    table.ForeignKey(
                        name: "FK_SellerPayouts_Libraries_LibraryId",
                        column: x => x.LibraryId,
                        principalTable: "Libraries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SellerPayouts_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SellerPayouts_LibraryId_CreationTime",
                table: "SellerPayouts",
                columns: new[] { "LibraryId", "CreationTime" });

            migrationBuilder.CreateIndex(
                name: "IX_SellerPayouts_OrderId_LibraryId",
                table: "SellerPayouts",
                columns: new[] { "OrderId", "LibraryId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SellerPayouts_Status_NextAttemptAtUtc",
                table: "SellerPayouts",
                columns: new[] { "Status", "NextAttemptAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SellerPayouts_StripeTransferId",
                table: "SellerPayouts",
                column: "StripeTransferId",
                unique: true,
                filter: "\"StripeTransferId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SellerPayouts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Libraries_ProfitSharePercent_Range",
                table: "Libraries");

            migrationBuilder.DropColumn(
                name: "ProfitSharePercent",
                table: "Libraries");

            migrationBuilder.DropColumn(
                name: "StripeWalletActivatedAtUtc",
                table: "Libraries");

            migrationBuilder.DropColumn(
                name: "StripeConnectAccountId",
                table: "Libraries");
        }
    }
}
