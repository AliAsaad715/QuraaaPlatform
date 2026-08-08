using FluentValidation;

namespace Quraaa.Application.Features.Libraries.Queries.GetLibraryRegistrationContext
{
    public sealed class GetLibraryRegistrationContextQueryValidator
        : AbstractValidator<GetLibraryRegistrationContextQuery>
    {
        public GetLibraryRegistrationContextQueryValidator()
        {
            RuleFor(x => x.Token)
                .NotEmpty()
                .MaximumLength(128);
        }
    }
}
