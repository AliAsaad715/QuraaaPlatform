using FluentValidation;

namespace Quraaa.Application.Features.Admin.Queries.GetUsers
{
    public sealed class GetUsersQueryValidator : AbstractValidator<GetUsersQuery>
    {
        public GetUsersQueryValidator()
        {
            RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);

            RuleFor(x => x.SearchTerm)
                .MaximumLength(200)
                .WithMessage("The search term cannot exceed 200 characters.");

            RuleFor(x => x.Role).IsInEnum().When(x => x.Role.HasValue);
        }
    }
}
