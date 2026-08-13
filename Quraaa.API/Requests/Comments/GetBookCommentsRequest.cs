namespace Quraaa.API.Requests.Comments
{
    public class GetBookCommentsRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
