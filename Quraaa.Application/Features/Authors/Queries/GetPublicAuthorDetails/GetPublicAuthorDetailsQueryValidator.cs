using FluentValidation;

namespace Quraaa.Application.Features.Authors.Queries.GetPublicAuthorDetails
{
    public sealed class GetPublicAuthorDetailsQueryValidator
        : AbstractValidator<GetPublicAuthorDetailsQuery>
    {
        public GetPublicAuthorDetailsQueryValidator()
        {
            RuleFor(query => query.AuthorId)
                .NotEmpty().WithMessage("Author id is required.");
        }
    }
}
