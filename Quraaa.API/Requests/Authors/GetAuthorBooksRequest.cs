namespace Quraaa.API.Requests.Authors
{
    public sealed record GetAuthorBooksRequest(
        int PageNumber = 1,
        int PageSize = 20,
        string? SearchTerm = null,
        string SortBy = "latest");
}
