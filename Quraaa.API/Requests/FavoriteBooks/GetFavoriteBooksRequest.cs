namespace Quraaa.API.Requests.FavoriteBooks
{
    public class GetFavoriteBooksRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SearchTerm { get; set; }
    }
}
