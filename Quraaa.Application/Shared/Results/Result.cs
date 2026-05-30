using FluentValidation.Results;

namespace Quraaa.Application.Shared.Results
{
    // ==========================================
    // 1. THE RETURN TYPES (The Data Holders)
    // ==========================================

    /// <summary>
    /// Represents a successful operation.
    /// It is a struct to avoid memory allocation (high performance).
    /// </summary>
    public struct Success { }

    /// <summary>
    /// Represents a failure due to Invalid Data (Input Validation).
    /// </summary>
    public record ValidationFailed(List<ValidationFailure> Errors);

    /// <summary>
    /// Represents a failure due to Business Logic (Domain Rules).
    /// </summary>
    public record DomainError(string Message);

    /// <summary>
    /// Represents a failure when an entity is not found (404).
    /// </summary>
    //public record NotFound;

    public record NotFound(string Message = "Resource Not Found");
    /// <summary>
    /// Represents a failure due to insufficient permissions (403).
    /// Example: User A trying to edit User B's data.
    /// </summary>
    public record Forbidden;

    /// <summary>
    /// Represents a failure due to missing identity (401).
    /// (Rarely used in Service layer if [Authorize] is used on Controller)
    /// </summary>
    public record Unauthorized;

    // ==========================================
    // 2. THE FACTORY (The Readable Syntax)
    // ==========================================

    public static class Result
    {
        /// <summary>
        /// Returns a Success signal.
        /// Usage: return Result.Success();
        /// </summary>
        public static Success Success() => new Success();

        /// <summary>
        /// Returns a Validation Failure with a list of errors.
        /// Usage: return Result.ValidationFailed(validationResult.Errors);
        /// </summary>
        public static ValidationFailed ValidationFailed(List<ValidationFailure> errors)
            => new ValidationFailed(errors);

        /// <summary>
        /// Returns a single Validation Failure (helper for manual checks).
        /// </summary>
        public static ValidationFailed ValidationFailed(string property, string error)
            => new ValidationFailed(new List<ValidationFailure>
            {
            new ValidationFailure(property, error)
            });

        /// <summary>
        /// Returns a Domain Error (Business Rule Violation).
        /// Usage: return Result.DomainError("Salary cannot be negative");
        /// </summary>
        public static DomainError DomainError(string message)
            => new DomainError(message);

        /// <summary>
        /// Returns a Not Found error.
        /// Usage: return Result.NotFound();
        /// </summary>
        public static NotFound NotFound(string message = "Resource Not Found") => new NotFound(message);

        public static Forbidden Forbidden() => new Forbidden();
        public static Unauthorized Unauthorized() => new Unauthorized();
    }
}
