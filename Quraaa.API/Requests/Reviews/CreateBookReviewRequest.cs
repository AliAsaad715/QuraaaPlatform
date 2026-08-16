namespace Quraaa.API.Requests.Reviews
{
    public class CreateBookReviewRequest
    {
        public int Score { get; set; }
        public string Content { get; set; } = string.Empty;
    }
}
