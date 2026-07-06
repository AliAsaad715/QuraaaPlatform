namespace Quraaa.API.Requests.Books
{
    public class GetMostPopularBooksRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SearchTerm { get; set; }
        public string SortBy { get; set; } = "popular";
        public bool IncludeUnranked { get; set; } = true;
    }
}
