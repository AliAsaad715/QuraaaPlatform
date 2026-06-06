namespace Quraaa.Application.Shared.Exceptions
{
    /// <summary>
    /// Represents an error that occurs due to application-level business rules (e.g., uniqueness, validation checks).
    /// </summary>
    public class ApplicationBusinessException : Exception
    {
        public ApplicationBusinessException(string message) : base(message)
        {
        }
    }
}
