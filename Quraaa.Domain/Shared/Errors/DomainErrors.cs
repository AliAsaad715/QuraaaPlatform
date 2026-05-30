namespace Quraaa.Domain.Shared.Errors
{
    // 1. Base Record
    public abstract record DomainError(string Code, string Message);

    // 2. Specific Error Types
    public record NotFoundError(string Code, string Message) : DomainError(Code, Message)
    {
        public NotFoundError(string message) : this("Resource.NotFound", message) { }
    }

    public record ConflictError(string Code, string Message) : DomainError(Code, Message)
    {
        public ConflictError(string message) : this("Resource.Conflict", message) { }
    }

    public record ValidationError(string Code, string Message, Dictionary<string, string[]> Errors) : DomainError(Code, Message);
}
