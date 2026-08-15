using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Quraaa.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ConsolidateUserDeviceTokensIntoPushDevices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $migration$
                BEGIN
                    IF EXISTS
                    (
                        SELECT 1
                        FROM "UserDeviceTokens" AS source
                        LEFT JOIN "UsersProfiles" AS profile
                            ON profile."Id" = source."UserId"
                        WHERE profile."Id" IS NULL
                    ) THEN
                        RAISE EXCEPTION
                            'Cannot consolidate UserDeviceTokens because at least one owner has no UsersProfiles row.';
                    END IF;

                    IF EXISTS
                    (
                        SELECT 1
                        FROM "UserDeviceTokens" AS source
                        WHERE btrim(source."DeviceToken") = ''
                           OR source."DeviceToken" ~ '[[:space:]]'
                           OR source."DeviceToken" ~ '[[:cntrl:]]'
                    ) THEN
                        RAISE EXCEPTION
                            'Cannot consolidate UserDeviceTokens because at least one token is invalid.';
                    END IF;

                    IF EXISTS
                    (
                        SELECT source_hash."TokenHash"
                        FROM
                        (
                            SELECT upper(encode(
                                sha256(convert_to(source."DeviceToken", 'UTF8')),
                                'hex')) AS "TokenHash"
                            FROM "UserDeviceTokens" AS source
                        ) AS source_hash
                        GROUP BY source_hash."TokenHash"
                        HAVING count(*) > 1
                    ) THEN
                        RAISE EXCEPTION
                            'Cannot consolidate UserDeviceTokens because two raw tokens share a SHA-256 hash.';
                    END IF;

                    IF EXISTS
                    (
                        SELECT 1
                        FROM "UserDeviceTokens" AS source
                        INNER JOIN "PushDevices" AS target
                            ON target."TokenHash" = upper(encode(
                                sha256(convert_to(source."DeviceToken", 'UTF8')),
                                'hex'))
                        WHERE target."Token" <> source."DeviceToken"
                    ) THEN
                        RAISE EXCEPTION
                            'Cannot consolidate UserDeviceTokens because a SHA-256 collision was detected.';
                    END IF;

                    IF EXISTS
                    (
                        SELECT 1
                        FROM "UserDeviceTokens" AS source
                        INNER JOIN "PushDevices" AS target
                            ON target."Id" = source."Id"
                        WHERE target."Token" <> source."DeviceToken"
                    ) THEN
                        RAISE EXCEPTION
                            'Cannot consolidate UserDeviceTokens because an identifier collision was detected.';
                    END IF;
                END
                $migration$;

                INSERT INTO "PushDevices"
                    ("Id", "UserId", "Token", "TokenHash", "CreationTime", "LastModificationTime")
                SELECT
                    source."Id",
                    source."UserId",
                    source."DeviceToken",
                    upper(encode(
                        sha256(convert_to(source."DeviceToken", 'UTF8')),
                        'hex')),
                    source."RegisteredAtUtc",
                    greatest(source."RegisteredAtUtc", source."LastSeenAtUtc")
                FROM "UserDeviceTokens" AS source
                ON CONFLICT ("TokenHash") DO UPDATE
                SET "UserId" = EXCLUDED."UserId",
                    "Token" = EXCLUDED."Token",
                    "LastModificationTime" = EXCLUDED."LastModificationTime"
                WHERE "PushDevices"."Token" = EXCLUDED."Token"
                  AND EXCLUDED."LastModificationTime" > coalesce(
                      "PushDevices"."LastModificationTime",
                      "PushDevices"."CreationTime");

                WITH ranked_devices AS
                (
                    SELECT
                        device."Id",
                        row_number() OVER
                        (
                            PARTITION BY device."UserId"
                            ORDER BY
                                coalesce(
                                    device."LastModificationTime",
                                    device."CreationTime") DESC,
                                device."Id" DESC
                        ) AS "DeviceRank"
                    FROM "PushDevices" AS device
                )
                DELETE FROM "PushDevices" AS device
                USING ranked_devices AS ranked
                WHERE device."Id" = ranked."Id"
                  AND ranked."DeviceRank" > 10;
                """);

            migrationBuilder.DropTable(
                name: "UserDeviceTokens");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserDeviceTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceToken = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: false),
                    LastSeenAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RegisteredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDeviceTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserDeviceTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserDeviceTokens_DeviceToken",
                table: "UserDeviceTokens",
                column: "DeviceToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserDeviceTokens_UserId",
                table: "UserDeviceTokens",
                column: "UserId");

            migrationBuilder.Sql(
                """
                INSERT INTO "UserDeviceTokens"
                    ("Id", "UserId", "DeviceToken", "RegisteredAtUtc", "LastSeenAtUtc")
                SELECT
                    device."Id",
                    device."UserId",
                    device."Token",
                    device."CreationTime",
                    coalesce(device."LastModificationTime", device."CreationTime")
                FROM "PushDevices" AS device;
                """);
        }
    }
}
