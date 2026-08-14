namespace Quraaa.Application.Features.Admin.Common
{
    public record AdminDashboardStatsDto(
        LibraryStatsDto Libraries,
        CatalogStatsDto Catalog,
        UserStatsDto Users,
        OrderStatsDto Orders
    );

    public record LibraryStatsDto(
        int TotalLibrariesCount
    );

    public record CatalogStatsDto(
        int TotalBooksCount,
        int TotalActiveListingsCount
    );

    public record UserStatsDto(
        int TotalUsersCount,
        int NewUsersThisMonth
    );

    public record OrderStatsDto(
        int TotalOrdersCount,
        int PendingOrdersCount
    );
}
