namespace Quraaa.API.Requests.Libraries
{
    public class RegisterLibraryRequest
    {
        public string Token { get; set; } = string.Empty;
        public string LibraryName { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public IFormFile? LibraryImage { get; set; }
        public IFormFile? HeaderImage { get; set; }
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Password for the library dashboard. Must differ from the owner's
        /// personal account password.
        /// </summary>
        public string Password { get; set; } = string.Empty;

        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
