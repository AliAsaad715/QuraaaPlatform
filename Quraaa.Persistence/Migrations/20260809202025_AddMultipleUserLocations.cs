using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Quraaa.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMultipleUserLocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The legacy columns had no pair/range constraints. Abort instead of
            // silently dropping a malformed coordinate during the table conversion.
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM "UsersProfiles"
                        WHERE ("Latitude" IS NULL) <> ("Longitude" IS NULL)
                           OR "Latitude" < -90
                           OR "Latitude" > 90
                           OR "Longitude" < -180
                           OR "Longitude" > 180
                           OR "Latitude"::text IN ('NaN', 'Infinity', '-Infinity')
                           OR "Longitude"::text IN ('NaN', 'Infinity', '-Infinity')
                    ) THEN
                        RAISE EXCEPTION 'Cannot migrate profile locations because one or more legacy coordinate pairs are incomplete or invalid.';
                    END IF;
                END $$;
                """);

            migrationBuilder.AddColumn<Guid>(
                name: "DefaultLocationId",
                table: "UsersProfiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LocationConcurrencyStamp",
                table: "UsersProfiles",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "UserLocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Address = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLocations", x => x.Id);
                    table.CheckConstraint("CK_UserLocations_Latitude_Valid", "\"Latitude\" BETWEEN -90 AND 90");
                    table.CheckConstraint("CK_UserLocations_Longitude_Valid", "\"Longitude\" BETWEEN -180 AND 180");
                    table.CheckConstraint("CK_UserLocations_Name_NotBlank", "btrim(\"Name\") <> ''");
                    table.ForeignKey(
                        name: "FK_UserLocations_UsersProfiles_UserId",
                        column: x => x.UserId,
                        principalTable: "UsersProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UsersProfiles_DefaultLocationId",
                table: "UsersProfiles",
                column: "DefaultLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLocations_UserId",
                table: "UserLocations",
                column: "UserId");

            // The destination table is new, so the profile id is a deterministic,
            // extension-free id for its migrated location. It also seeds a distinct
            // concurrency stamp for every existing profile.
            migrationBuilder.Sql(
                """
                UPDATE "UsersProfiles"
                SET "LocationConcurrencyStamp" = "Id";

                INSERT INTO "UserLocations" (
                    "Id",
                    "UserId",
                    "Name",
                    "Address",
                    "Latitude",
                    "Longitude",
                    "CreationTime",
                    "LastModificationTime")
                SELECT
                    "Id",
                    "Id",
                    'Saved location',
                    NULL,
                    "Latitude",
                    "Longitude",
                    "CreationTime",
                    NULL
                FROM "UsersProfiles"
                WHERE "Latitude" IS NOT NULL
                  AND "Longitude" IS NOT NULL;

                UPDATE "UsersProfiles"
                SET "DefaultLocationId" = "Id"
                WHERE "Latitude" IS NOT NULL
                  AND "Longitude" IS NOT NULL;
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_UsersProfiles_UserLocations_DefaultLocationId",
                table: "UsersProfiles",
                column: "DefaultLocationId",
                principalTable: "UserLocations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // The regular FK guarantees that the selected row exists and provides
            // ON DELETE SET NULL. This trigger additionally guarantees ownership.
            // A composite SET NULL FK is not suitable because it would also attempt
            // to null the profile's non-null Id column.
            migrationBuilder.Sql(
                """
                CREATE FUNCTION enforce_profile_default_location_owner()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $function$
                BEGIN
                    IF NEW."DefaultLocationId" IS NOT NULL
                       AND NOT EXISTS (
                           SELECT 1
                           FROM "UserLocations" AS locations
                           WHERE locations."Id" = NEW."DefaultLocationId"
                             AND locations."UserId" = NEW."Id"
                       ) THEN
                        RAISE EXCEPTION 'The default location must belong to the same user profile.'
                            USING ERRCODE = '23514',
                                  CONSTRAINT = 'CK_UsersProfiles_DefaultLocation_Owner';
                    END IF;

                    RETURN NEW;
                END;
                $function$;

                CREATE TRIGGER "TR_UsersProfiles_DefaultLocation_Owner"
                BEFORE INSERT OR UPDATE OF "DefaultLocationId"
                ON "UsersProfiles"
                FOR EACH ROW
                EXECUTE FUNCTION enforce_profile_default_location_owner();
                """);

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "UsersProfiles");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "UsersProfiles");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TRIGGER IF EXISTS "TR_UsersProfiles_DefaultLocation_Owner"
                    ON "UsersProfiles";
                DROP FUNCTION IF EXISTS enforce_profile_default_location_owner();
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_UsersProfiles_UserLocations_DefaultLocationId",
                table: "UsersProfiles");

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "UsersProfiles",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "UsersProfiles",
                type: "double precision",
                nullable: true);

            // A downgrade can retain only one location. Prefer the valid default;
            // otherwise preserve the oldest location deterministically.
            migrationBuilder.Sql(
                """
                UPDATE "UsersProfiles" AS users
                SET ("Latitude", "Longitude") = (
                    SELECT locations."Latitude", locations."Longitude"
                    FROM "UserLocations" AS locations
                    WHERE locations."UserId" = users."Id"
                    ORDER BY
                        (locations."Id" = users."DefaultLocationId") DESC NULLS LAST,
                        locations."CreationTime",
                        locations."Id"
                    LIMIT 1
                )
                WHERE EXISTS (
                    SELECT 1
                    FROM "UserLocations" AS locations
                    WHERE locations."UserId" = users."Id"
                );
                """);

            migrationBuilder.DropTable(
                name: "UserLocations");

            migrationBuilder.DropIndex(
                name: "IX_UsersProfiles_DefaultLocationId",
                table: "UsersProfiles");

            migrationBuilder.DropColumn(
                name: "DefaultLocationId",
                table: "UsersProfiles");

            migrationBuilder.DropColumn(
                name: "LocationConcurrencyStamp",
                table: "UsersProfiles");
        }
    }
}
