namespace Quraaa.API.Requests.Reviews
{
    public class GetBookReviewsRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
