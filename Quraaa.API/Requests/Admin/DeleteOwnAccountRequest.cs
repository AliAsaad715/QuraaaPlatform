namespace Quraaa.API.Requests.Admin
{
    /// <summary>
    /// Confirmation for deleting your own account: the password proves it is
    /// you, the phrase proves you meant it.
    /// </summary>
    public class DeleteOwnAccountRequest
    {
        public string Password { get; set; } = string.Empty;

        /// <summary>Must read exactly "DELETE MY ACCOUNT".</summary>
        public string ConfirmationPhrase { get; set; } = string.Empty;
    }
}
