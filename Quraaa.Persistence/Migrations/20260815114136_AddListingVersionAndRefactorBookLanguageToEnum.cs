using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Quraaa.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddListingVersionAndRefactorBookLanguageToEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Listings",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            // "IX_Books_Title_Author_Language_CI" is a functional index on lower("Language"),
            // which only accepts text. It must be dropped before the column type changes to
            // integer below, otherwise Postgres cannot re-bind lower(...) to an integer column.
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Books_Title_Author_Language_CI"";");

            // Normalize every existing free-text Language value (column is still varchar here)
            // to the string form of its target enum value, so the explicit USING cast below can
            // parse every row safely. NULL/blank/unrecognized values default to Language.Other
            // (99) so no row is lost or left invalid.
            migrationBuilder.Sql(@"
                UPDATE ""Books""
                SET ""Language"" = CASE
                    WHEN ""Language"" IS NULL OR trim(""Language"") = '' THEN '99'
                    WHEN lower(trim(""Language"")) IN ('arabic', 'ar', 'العربية', '1') THEN '1'
                    WHEN lower(trim(""Language"")) IN ('english', 'en', 'الإنجليزية', '2') THEN '2'
                    WHEN lower(trim(""Language"")) IN ('french', 'fr', 'الفرنسية', '3') THEN '3'
                    ELSE '99'
                END;
            ");

            // Postgres has no automatic assignment cast from character varying to integer, so
            // migrationBuilder.AlterColumn<int> alone fails with 42804 ("column ... cannot be
            // cast automatically to type integer"). Every value is now a plain digit string
            // ('1'/'2'/'3'/'99'), so the explicit USING cast below always succeeds.
            migrationBuilder.Sql(@"
                ALTER TABLE ""Books""
                ALTER COLUMN ""Language"" TYPE integer
                USING ""Language""::integer;
            ");

            // "Books"."Author" no longer exists here: migration RefactorBookAuthorToForeignKeyAndAddBirthDate
            // (which runs before this one) replaced it with "AuthorId" (uuid FK to "Authors"), so the
            // recreated index keys on AuthorId directly instead of lower("Author").
            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX ""IX_Books_Title_Author_Language_CI""
                ON ""Books"" (lower(""Title""), ""AuthorId"", ""Language"");
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Version",
                table: "Listings");

            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Books_Title_Author_Language_CI"";");

            // Same 42804 concern as Up, in reverse: cast explicitly instead of relying on
            // migrationBuilder.AlterColumn<string> to infer a USING clause.
            migrationBuilder.Sql(@"
                ALTER TABLE ""Books""
                ALTER COLUMN ""Language"" TYPE character varying(20)
                USING ""Language""::text;
            ");

            // Restore human-readable defaults on rollback. Any value outside the known enum set
            // (defensively; only 1/2/3/99 should exist) also falls back to 'Other'.
            migrationBuilder.Sql(@"
                UPDATE ""Books""
                SET ""Language"" = CASE ""Language""
                    WHEN '1' THEN 'Arabic'
                    WHEN '2' THEN 'English'
                    WHEN '3' THEN 'French'
                    ELSE 'Other'
                END;
            ");

            // Same AuthorId note as in Up: "Author" doesn't exist on "Books" at this point in the
            // migration chain, so this index (like the one it replaces) keys on AuthorId directly.
            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX ""IX_Books_Title_Author_Language_CI""
                ON ""Books"" (lower(""Title""), ""AuthorId"", lower(""Language""));
            ");
        }
    }
}
