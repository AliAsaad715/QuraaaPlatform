namespace Quraaa.API.Requests.Books
{
    public class SetBookVisibilityRequest
    {
        /// <summary>True withholds the book from the catalogue; false returns it.</summary>
        public bool Hidden { get; set; }

        public string? ModerationNote { get; set; }
    }
}
