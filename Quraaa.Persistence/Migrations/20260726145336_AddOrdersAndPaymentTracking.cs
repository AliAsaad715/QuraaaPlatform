using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Quraaa.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrdersAndPaymentTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PendingOrderId",
                table: "Carts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrderId",
                table: "BookPurchases",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrderItemId",
                table: "BookPurchases",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BuyerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceCartId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PaymentStatus = table.Column<int>(type: "integer", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    SubtotalAmountMinor = table.Column<long>(type: "bigint", nullable: false),
                    ShippingAmountMinor = table.Column<long>(type: "bigint", nullable: false),
                    DiscountAmountMinor = table.Column<long>(type: "bigint", nullable: false),
                    TotalAmountMinor = table.Column<long>(type: "bigint", nullable: false),
                    ShippingLatitude = table.Column<double>(type: "double precision", nullable: true),
                    ShippingLongitude = table.Column<double>(type: "double precision", nullable: true),
                    PaidAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancellationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeleationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                    table.CheckConstraint("CK_Orders_DiscountAmountMinor_NonNegative", "\"DiscountAmountMinor\" >= 0");
                    table.CheckConstraint("CK_Orders_ShippingAmountMinor_NonNegative", "\"ShippingAmountMinor\" >= 0");
                    table.CheckConstraint("CK_Orders_ShippingLocation_Valid", "(\"ShippingLatitude\" IS NULL AND \"ShippingLongitude\" IS NULL) OR (\"ShippingLatitude\" BETWEEN -90 AND 90 AND \"ShippingLongitude\" BETWEEN -180 AND 180)");
                    table.CheckConstraint("CK_Orders_SubtotalAmountMinor_Positive", "\"SubtotalAmountMinor\" > 0");
                    table.CheckConstraint("CK_Orders_TotalAmountMinor_Consistent", "\"TotalAmountMinor\" = \"SubtotalAmountMinor\" + \"ShippingAmountMinor\" - \"DiscountAmountMinor\"");
                    table.CheckConstraint("CK_Orders_TotalAmountMinor_Positive", "\"TotalAmountMinor\" > 0");
                });

            migrationBuilder.CreateTable(
                name: "ProcessedPaymentEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EventId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    EventType = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    PaymentAttemptId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProcessedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessedPaymentEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrderItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    BookId = table.Column<Guid>(type: "uuid", nullable: false),
                    ListingId = table.Column<Guid>(type: "uuid", nullable: false),
                    SellerType = table.Column<int>(type: "integer", nullable: false),
                    SellerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Format = table.Column<int>(type: "integer", nullable: false),
                    BookTitleSnapshot = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    BookAuthorSnapshot = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    BookCoverImageUrlSnapshot = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SellerNameSnapshot = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    DigitalAssetUrlSnapshot = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    ConditionSnapshot = table.Column<int>(type: "integer", nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UnitPriceMinor = table.Column<long>(type: "bigint", nullable: false),
                    TotalPriceMinor = table.Column<long>(type: "bigint", nullable: false),
                    FulfillmentStatus = table.Column<int>(type: "integer", nullable: false),
                    FulfilledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItems", x => x.Id);
                    table.CheckConstraint("CK_OrderItems_FormatSnapshot_Valid", "(\"Format\" = 1 AND \"DigitalAssetUrlSnapshot\" IS NOT NULL AND \"ConditionSnapshot\" IS NULL) OR (\"Format\" = 2 AND \"DigitalAssetUrlSnapshot\" IS NULL AND \"ConditionSnapshot\" IS NOT NULL)");
                    table.CheckConstraint("CK_OrderItems_Quantity_Positive", "\"Quantity\" > 0");
                    table.CheckConstraint("CK_OrderItems_SellerType_Valid", "\"SellerType\" IN (1, 2)");
                    table.CheckConstraint("CK_OrderItems_TotalPriceMinor_Consistent", "\"TotalPriceMinor\" = \"UnitPriceMinor\" * \"Quantity\"");
                    table.CheckConstraint("CK_OrderItems_TotalPriceMinor_Positive", "\"TotalPriceMinor\" > 0");
                    table.CheckConstraint("CK_OrderItems_UnitPriceMinor_Positive", "\"UnitPriceMinor\" > 0");
                    table.ForeignKey(
                        name: "FK_OrderItems_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderPaymentAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttemptNumber = table.Column<int>(type: "integer", nullable: false),
                    Provider = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AmountMinor = table.Column<long>(type: "bigint", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    SuccessUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    CancelUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    CheckoutSessionId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CheckoutUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    PaymentIntentId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailureCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FailureMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderPaymentAttempts", x => x.Id);
                    table.CheckConstraint("CK_OrderPaymentAttempts_AmountMinor_Positive", "\"AmountMinor\" > 0");
                    table.CheckConstraint("CK_OrderPaymentAttempts_AttemptNumber_Positive", "\"AttemptNumber\" > 0");
                    table.CheckConstraint("CK_OrderPaymentAttempts_CheckoutFields_Paired", "(\"CheckoutSessionId\" IS NULL AND \"CheckoutUrl\" IS NULL) OR (\"CheckoutSessionId\" IS NOT NULL AND \"CheckoutUrl\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_OrderPaymentAttempts_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Carts_PendingOrderId",
                table: "Carts",
                column: "PendingOrderId",
                unique: true,
                filter: "\"PendingOrderId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BookPurchases_OrderId",
                table: "BookPurchases",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_BookPurchases_OrderItemId",
                table: "BookPurchases",
                column: "OrderItemId",
                unique: true,
                filter: "\"OrderItemId\" IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_BookPurchases_OrderReferences_Paired",
                table: "BookPurchases",
                sql: "(\"OrderId\" IS NULL AND \"OrderItemId\" IS NULL) OR (\"OrderId\" IS NOT NULL AND \"OrderItemId\" IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_BookId",
                table: "OrderItems",
                column: "BookId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_ListingId",
                table: "OrderItems",
                column: "ListingId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_OrderId_ListingId",
                table: "OrderItems",
                columns: new[] { "OrderId", "ListingId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_SellerId",
                table: "OrderItems",
                column: "SellerId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_SellerId_FulfillmentStatus",
                table: "OrderItems",
                columns: new[] { "SellerId", "FulfillmentStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_SellerType_SellerId",
                table: "OrderItems",
                columns: new[] { "SellerType", "SellerId" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderPaymentAttempts_CheckoutSessionId",
                table: "OrderPaymentAttempts",
                column: "CheckoutSessionId",
                unique: true,
                filter: "\"CheckoutSessionId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OrderPaymentAttempts_ExpiresAtUtc",
                table: "OrderPaymentAttempts",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_OrderPaymentAttempts_OrderId_AttemptNumber",
                table: "OrderPaymentAttempts",
                columns: new[] { "OrderId", "AttemptNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderPaymentAttempts_OrderId_Status",
                table: "OrderPaymentAttempts",
                columns: new[] { "OrderId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderPaymentAttempts_PaymentIntentId",
                table: "OrderPaymentAttempts",
                column: "PaymentIntentId",
                unique: true,
                filter: "\"PaymentIntentId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_BuyerUserId_CreationTime",
                table: "Orders",
                columns: new[] { "BuyerUserId", "CreationTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_BuyerUserId_Status_CreationTime",
                table: "Orders",
                columns: new[] { "BuyerUserId", "Status", "CreationTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrderNumber",
                table: "Orders",
                column: "OrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_PaidAtUtc",
                table: "Orders",
                column: "PaidAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_PaymentStatus",
                table: "Orders",
                column: "PaymentStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_SourceCartId",
                table: "Orders",
                column: "SourceCartId",
                unique: true,
                filter: "\"IsDeleted\" = false AND \"Status\" IN (1, 2)");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Status",
                table: "Orders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedPaymentEvents_OrderId",
                table: "ProcessedPaymentEvents",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedPaymentEvents_PaymentAttemptId",
                table: "ProcessedPaymentEvents",
                column: "PaymentAttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedPaymentEvents_ProcessedAtUtc",
                table: "ProcessedPaymentEvents",
                column: "ProcessedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedPaymentEvents_Provider_EventId",
                table: "ProcessedPaymentEvents",
                columns: new[] { "Provider", "EventId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderItems");

            migrationBuilder.DropTable(
                name: "OrderPaymentAttempts");

            migrationBuilder.DropTable(
                name: "ProcessedPaymentEvents");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Carts_PendingOrderId",
                table: "Carts");

            migrationBuilder.DropIndex(
                name: "IX_BookPurchases_OrderId",
                table: "BookPurchases");

            migrationBuilder.DropIndex(
                name: "IX_BookPurchases_OrderItemId",
                table: "BookPurchases");

            migrationBuilder.DropCheckConstraint(
                name: "CK_BookPurchases_OrderReferences_Paired",
                table: "BookPurchases");

            migrationBuilder.DropColumn(
                name: "PendingOrderId",
                table: "Carts");

            migrationBuilder.DropColumn(
                name: "OrderId",
                table: "BookPurchases");

            migrationBuilder.DropColumn(
                name: "OrderItemId",
                table: "BookPurchases");
        }
    }
}
