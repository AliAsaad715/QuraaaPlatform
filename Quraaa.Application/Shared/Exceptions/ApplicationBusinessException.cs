namespace Quraaa.Application.Shared.Exceptions
{
    /// <summary>
    /// Represents an error that occurs due to application-level business rules (e.g., uniqueness, validation checks).
    /// </summary>
    public class ApplicationBusinessException : Exception
    {
        /// <summary>
        /// The request property this failure is about, for callers that want a specific
        /// field named in the 400 response (see BaseApplicationService's
        /// ApplicationBusinessException handling) instead of the generic default. Null
        /// for rule violations that were already shipping without one.
        /// </summary>
        public string? PropertyName { get; }

        public ApplicationBusinessException(string message) : base(message)
        {
        }

        public ApplicationBusinessException(string message, string propertyName) : base(message)
        {
            PropertyName = propertyName;
        }
    }
}
