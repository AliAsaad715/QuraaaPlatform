using OneOf;

namespace Quraaa.Application.Shared.Results
{
    /// <summary>
    /// A unified container that holds any of the possible result types.
    /// This acts as the standard return type for all application services.
    /// </summary>
    public class AppResult : OneOfBase<Success, ValidationFailed, NotFound, Forbidden, DomainError>
    {
        protected AppResult(OneOf<Success, ValidationFailed, NotFound, Forbidden, DomainError> input) : base(input) { }

        public static implicit operator AppResult(Success _) => new(_);
        public static implicit operator AppResult(ValidationFailed _) => new(_);
        public static implicit operator AppResult(NotFound _) => new(_);
        public static implicit operator AppResult(Forbidden _) => new(_);
        public static implicit operator AppResult(DomainError _) => new(_);
    }


    public class AppResult<TData> : OneOfBase<TData, ValidationFailed, NotFound, Forbidden, DomainError>
    {
        protected AppResult(OneOf<TData, ValidationFailed, NotFound, Forbidden, DomainError> input) : base(input) { }

        public static implicit operator AppResult<TData>(TData data) => new(data);
        public static implicit operator AppResult<TData>(ValidationFailed _) => new(OneOf<TData, ValidationFailed, NotFound, Forbidden, DomainError>.FromT1(_));
        public static implicit operator AppResult<TData>(NotFound _) => new(OneOf<TData, ValidationFailed, NotFound, Forbidden, DomainError>.FromT2(_));
        public static implicit operator AppResult<TData>(Forbidden _) => new(OneOf<TData, ValidationFailed, NotFound, Forbidden, DomainError>.FromT3(_));
        public static implicit operator AppResult<TData>(DomainError _) => new(OneOf<TData, ValidationFailed, NotFound, Forbidden, DomainError>.FromT4(_));
    }
}
