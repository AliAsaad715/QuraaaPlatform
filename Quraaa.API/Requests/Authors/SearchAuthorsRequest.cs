namespace Quraaa.API.Requests.Authors
{
    public sealed record SearchAuthorsRequest(
        string? SearchTerm = null,
        int PageNumber = 1,
        int PageSize = 10);
}
