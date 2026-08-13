using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Quraaa.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PreventUserLocationOwnerReassignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE FUNCTION prevent_user_location_owner_reassignment()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $function$
                BEGIN
                    IF NEW."UserId" IS DISTINCT FROM OLD."UserId" THEN
                        RAISE EXCEPTION 'A saved location cannot be reassigned to another user.'
                            USING ERRCODE = '23514',
                                  CONSTRAINT = 'CK_UserLocations_UserId_Immutable';
                    END IF;

                    RETURN NEW;
                END;
                $function$;

                CREATE TRIGGER "TR_UserLocations_UserId_Immutable"
                BEFORE UPDATE OF "UserId"
                ON "UserLocations"
                FOR EACH ROW
                EXECUTE FUNCTION prevent_user_location_owner_reassignment();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TRIGGER IF EXISTS "TR_UserLocations_UserId_Immutable"
                    ON "UserLocations";
                DROP FUNCTION IF EXISTS prevent_user_location_owner_reassignment();
                """);
        }
    }
}
