namespace Quraaa.API.Requests.Reviews
{
    public class UpdateBookReviewRequest
    {
        public int Score { get; set; }
        public string Content { get; set; } = string.Empty;
    }
}
