using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quraaa.API.Controllers;
using Quraaa.Domain.User.Enums;
using Quraaa.Persistence.Seed;

namespace Quraaa.API.IntegrationTests;

public sealed class RoleContractTests
{
    [Fact]
    public void RoleEnum_ContainsOnlyTheSupportedPersistentValues()
    {
        Assert.Equal(
            [nameof(Role.User), nameof(Role.LibraryOwner), nameof(Role.SuperAdmin)],
            Enum.GetNames<Role>());

        Assert.Equal(1, (int)Role.User);
        Assert.Equal(3, (int)Role.LibraryOwner);
        Assert.Equal(4, (int)Role.SuperAdmin);
        Assert.False(Enum.IsDefined((Role)2));
    }

    [Fact]
    public void ControllerRoleMetadata_UsesOnlySupportedRoles()
    {
        var supportedRoles = Enum.GetNames<Role>().ToHashSet(StringComparer.Ordinal);
        var invalidRoleReferences = typeof(Program).Assembly
            .GetTypes()
            .Where(type => typeof(ControllerBase).IsAssignableFrom(type))
            .SelectMany(type => GetAuthorizationMetadata(type))
            .SelectMany(metadata => metadata.Roles
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(role => new { metadata.Location, Role = role }))
            .Where(reference => !supportedRoles.Contains(reference.Role))
            .Select(reference => $"{reference.Location}: {reference.Role}")
            .OrderBy(reference => reference, StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            invalidRoleReferences.Length == 0,
            "Controller authorization references undefined roles: " +
            string.Join("; ", invalidRoleReferences));
    }

    [Fact]
    public void ExistingAdministrativeEndpoints_RequireSuperAdmin()
    {
        var administrativeEndpoints = new (Type Controller, string? Action)[]
        {
            (typeof(AdminAuthorsController), null),
            (typeof(AdminController), null),
            (typeof(AdminModerationController), null),
            (typeof(BookModerationController), null),
            (typeof(BookReportsController), nameof(BookReportsController.GetReports)),
            (typeof(BookReportsController), nameof(BookReportsController.GetReport)),
            (typeof(BookReportsController), nameof(BookReportsController.UpdateReportStatus)),
            (typeof(CategoriesController), nameof(CategoriesController.CreateCategory)),
            (typeof(CategoriesController), nameof(CategoriesController.UpdateCategory)),
            (typeof(CategoriesController), nameof(CategoriesController.DeleteCategory)),
            (typeof(LibrariesController), nameof(LibrariesController.GetRequests)),
            (typeof(LibrariesController), nameof(LibrariesController.UpdateApprovalStatus)),
            (typeof(LibrariesController), nameof(LibrariesController.GetProfitShare)),
            (typeof(LibrariesController), nameof(LibrariesController.SetProfitShare)),
        };

        Assert.All(administrativeEndpoints, endpoint =>
        {
            var attributes = endpoint.Action is null
                ? endpoint.Controller.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
                : endpoint.Controller.GetMethod(endpoint.Action)!
                    .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true);

            var roles = attributes
                .Cast<AuthorizeAttribute>()
                .Where(attribute => !string.IsNullOrWhiteSpace(attribute.Roles))
                .SelectMany(attribute => attribute.Roles!
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(role => role, StringComparer.Ordinal)
                .ToArray();

            Assert.Equal([nameof(Role.SuperAdmin)], roles);
        });
    }

    [Fact]
    public void DemoUsers_UseOnlySupportedRoles()
    {
        var supportedRoles = Enum.GetNames<Role>().ToHashSet(StringComparer.Ordinal);

        Assert.All(DemoSeedData.Users, user =>
        {
            Assert.Contains(user.DomainRole.ToString(), supportedRoles);
            Assert.All(user.IdentityRoles, role => Assert.Contains(role, supportedRoles));
        });

        var superAdmin = Assert.Single(
            DemoSeedData.Users,
            user => user.DomainRole == Role.SuperAdmin);
        Assert.Equal([nameof(Role.SuperAdmin)], superAdmin.IdentityRoles);
    }

    private static IEnumerable<(string Location, string Roles)> GetAuthorizationMetadata(Type controllerType)
    {
        foreach (var attribute in controllerType.GetCustomAttributes(
                     typeof(AuthorizeAttribute),
                     inherit: true).Cast<AuthorizeAttribute>())
        {
            if (!string.IsNullOrWhiteSpace(attribute.Roles))
            {
                yield return ($"{controllerType.Name}", attribute.Roles);
            }
        }

        foreach (var method in controllerType.GetMethods())
        {
            foreach (var attribute in method.GetCustomAttributes(
                         typeof(AuthorizeAttribute),
                         inherit: true).Cast<AuthorizeAttribute>())
            {
                if (!string.IsNullOrWhiteSpace(attribute.Roles))
                {
                    yield return ($"{controllerType.Name}.{method.Name}", attribute.Roles);
                }
            }
        }
    }
}
