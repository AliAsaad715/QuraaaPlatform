namespace Quraaa.API.Requests.Books
{
    public class GetRecommendedBooksRequest
    {
        public string Language { get; set; } = string.Empty;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SearchTerm { get; set; }
    }
}
