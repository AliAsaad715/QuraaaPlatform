using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Quraaa.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ConsolidateAdminIntoSuperAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $migration$
                DECLARE
                    legacy_admin_role_id uuid;
                    super_admin_role_id uuid;
                BEGIN
                    SELECT "Id"
                    INTO legacy_admin_role_id
                    FROM "AspNetRoles"
                    WHERE "NormalizedName" = 'ADMIN' OR "Name" = 'Admin'
                    ORDER BY CASE WHEN "NormalizedName" = 'ADMIN' THEN 0 ELSE 1 END
                    LIMIT 1;

                    SELECT "Id"
                    INTO super_admin_role_id
                    FROM "AspNetRoles"
                    WHERE "NormalizedName" = 'SUPERADMIN' OR "Name" = 'SuperAdmin'
                    ORDER BY CASE WHEN "NormalizedName" = 'SUPERADMIN' THEN 0 ELSE 1 END
                    LIMIT 1;

                    IF super_admin_role_id IS NULL THEN
                        IF legacy_admin_role_id IS NOT NULL THEN
                            -- When Admin is the only privileged Identity role,
                            -- renaming it preserves its memberships and claims.
                            UPDATE "AspNetRoles"
                            SET "Name" = 'SuperAdmin',
                                "NormalizedName" = 'SUPERADMIN'
                            WHERE "Id" = legacy_admin_role_id;

                            super_admin_role_id := legacy_admin_role_id;
                        ELSE
                            -- A clean database has no privileged account yet;
                            -- create the role before the bootstrap seeder runs.
                            super_admin_role_id := gen_random_uuid();

                            INSERT INTO "AspNetRoles"
                                ("Id", "Name", "NormalizedName", "ConcurrencyStamp")
                            VALUES
                                (super_admin_role_id, 'SuperAdmin', 'SUPERADMIN', super_admin_role_id::text);
                        END IF;
                    END IF;

                    -- Canonicalize a pre-existing target found through either
                    -- Name or NormalizedName so Identity can always resolve it.
                    UPDATE "AspNetRoles"
                    SET "Name" = 'SuperAdmin',
                        "NormalizedName" = 'SUPERADMIN',
                        "ConcurrencyStamp" = gen_random_uuid()::text
                    WHERE "Id" = super_admin_role_id;

                    -- A role elevation must not be inherited by an existing
                    -- refresh-token family. Also repair any SuperAdmin profile
                    -- that was created by the old inconsistent bootstrap seed
                    -- without the matching Identity role.
                    UPDATE "AspNetUsers" AS identity_user
                    SET "RefreshToken" = NULL,
                        "RefreshTokenExpiryTime" = CURRENT_TIMESTAMP,
                        "RefreshTokenFamilyId" = NULL
                    WHERE EXISTS
                    (
                        SELECT 1
                        FROM "UsersProfiles" AS profile
                        WHERE profile."Id" = identity_user."Id"
                          AND profile."Role" = 2
                    )
                    OR
                    (
                        legacy_admin_role_id IS NOT NULL
                        AND EXISTS
                        (
                            SELECT 1
                            FROM "AspNetUserRoles" AS user_role
                            WHERE user_role."UserId" = identity_user."Id"
                              AND user_role."RoleId" = legacy_admin_role_id
                        )
                    )
                    OR EXISTS
                    (
                        SELECT 1
                        FROM "UsersProfiles" AS profile
                        WHERE profile."Id" = identity_user."Id"
                          AND profile."Role" = 4
                          AND NOT EXISTS
                          (
                              SELECT 1
                              FROM "AspNetUserRoles" AS user_role
                              WHERE user_role."UserId" = identity_user."Id"
                                AND user_role."RoleId" = super_admin_role_id
                          )
                    );

                    -- Promote both domain Admin profiles and any profile whose
                    -- Identity account held the legacy Admin role.
                    UPDATE "UsersProfiles" AS profile
                    SET "Role" = 4
                    WHERE profile."Role" = 2
                       OR
                       (
                           legacy_admin_role_id IS NOT NULL
                           AND EXISTS
                           (
                               SELECT 1
                               FROM "AspNetUserRoles" AS user_role
                               WHERE user_role."UserId" = profile."Id"
                                 AND user_role."RoleId" = legacy_admin_role_id
                           )
                       );

                    -- Every domain SuperAdmin must have the matching Identity
                    -- role so login checks and endpoint authorization agree.
                    INSERT INTO "AspNetUserRoles" ("UserId", "RoleId")
                    SELECT profile."Id", super_admin_role_id
                    FROM "UsersProfiles" AS profile
                    INNER JOIN "AspNetUsers" AS identity_user
                        ON identity_user."Id" = profile."Id"
                    WHERE profile."Role" = 4
                    ON CONFLICT ("UserId", "RoleId") DO NOTHING;

                    IF legacy_admin_role_id IS NOT NULL
                       AND legacy_admin_role_id <> super_admin_role_id THEN
                        -- Preserve privileged Identity-only accounts too.
                        INSERT INTO "AspNetUserRoles" ("UserId", "RoleId")
                        SELECT user_role."UserId", super_admin_role_id
                        FROM "AspNetUserRoles" AS user_role
                        WHERE user_role."RoleId" = legacy_admin_role_id
                        ON CONFLICT ("UserId", "RoleId") DO NOTHING;

                        -- Role claims are not currently used for authorization,
                        -- but retaining them avoids silently dropping permissions.
                        INSERT INTO "AspNetRoleClaims" ("RoleId", "ClaimType", "ClaimValue")
                        SELECT super_admin_role_id,
                               legacy_claim."ClaimType",
                               legacy_claim."ClaimValue"
                        FROM "AspNetRoleClaims" AS legacy_claim
                        WHERE legacy_claim."RoleId" = legacy_admin_role_id
                          AND NOT EXISTS
                          (
                              SELECT 1
                              FROM "AspNetRoleClaims" AS current_claim
                              WHERE current_claim."RoleId" = super_admin_role_id
                                AND current_claim."ClaimType"
                                    IS NOT DISTINCT FROM legacy_claim."ClaimType"
                                AND current_claim."ClaimValue"
                                    IS NOT DISTINCT FROM legacy_claim."ClaimValue"
                          );

                        -- Cascades remove the obsolete memberships and claims
                        -- after their SuperAdmin equivalents have been written.
                        DELETE FROM "AspNetRoles"
                        WHERE "Id" = legacy_admin_role_id;
                    END IF;
                END
                $migration$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException(
                "Admin-to-SuperAdmin consolidation cannot be reversed because " +
                "the database no longer records which SuperAdmins were legacy Admins.");
        }
    }
}
