namespace Quraaa.API.Requests.Listings
{
    public record GetMyListingsRequest(string? SearchTerm, string? SortBy, bool SortDescending);
}
