using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Quraaa.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RefactorBookAuthorToForeignKeyAndAddBirthDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "BirthDate",
                table: "Authors",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AuthorId",
                table: "Books",
                type: "uuid",
                nullable: true);

            // Backfill: create an Author row for every distinct existing Books.Author
            // value that doesn't already match an existing Author name, then link each
            // book to its matching Author by normalized (trimmed, case-insensitive) name.
            // This runs before the old Author column is dropped below so no book loses
            // its author information. gen_random_uuid() is a PostgreSQL 13+ core builtin.
            migrationBuilder.Sql(
                """
                INSERT INTO "Authors" ("Id", "Name", "CreationTime", "IsDeleted")
                SELECT gen_random_uuid(), distinct_authors."Name", NOW(), FALSE
                FROM (
                    SELECT DISTINCT TRIM("Author") AS "Name"
                    FROM "Books"
                    WHERE "Author" IS NOT NULL AND TRIM("Author") <> ''
                ) AS distinct_authors
                WHERE NOT EXISTS (
                    SELECT 1 FROM "Authors" a WHERE LOWER(a."Name") = LOWER(distinct_authors."Name")
                );

                UPDATE "Books" b
                SET "AuthorId" = a."Id"
                FROM "Authors" a
                WHERE LOWER(TRIM(b."Author")) = LOWER(TRIM(a."Name"))
                  AND b."Author" IS NOT NULL
                  AND TRIM(b."Author") <> '';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Books_AuthorId",
                table: "Books",
                column: "AuthorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Books_Authors_AuthorId",
                table: "Books",
                column: "AuthorId",
                principalTable: "Authors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.DropColumn(
                name: "Author",
                table: "Books");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Author",
                table: "Books",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            // Best-effort reverse: copy each linked Author's name back onto Books.Author.
            // A book with no linked author keeps the "" default from the column above.
            migrationBuilder.Sql(
                """
                UPDATE "Books" b
                SET "Author" = a."Name"
                FROM "Authors" a
                WHERE b."AuthorId" = a."Id";
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_Books_Authors_AuthorId",
                table: "Books");

            migrationBuilder.DropIndex(
                name: "IX_Books_AuthorId",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "AuthorId",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "BirthDate",
                table: "Authors");
        }
    }
}
