namespace Quraaa.API.Requests.Libraries
{
    public sealed record SearchLibrariesRequest(
        string? SearchTerm = null,
        int PageNumber = 1,
        int PageSize = 10);
}
