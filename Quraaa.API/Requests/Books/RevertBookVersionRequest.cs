namespace Quraaa.API.Requests.Books
{
    public class RevertBookVersionRequest
    {
        /// <summary>The earlier version whose content should become current.</summary>
        public int VersionNumber { get; set; }

        public string? ModerationNote { get; set; }
    }
}
