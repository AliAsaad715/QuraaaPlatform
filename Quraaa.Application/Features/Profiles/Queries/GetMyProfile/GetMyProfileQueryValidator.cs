using FluentValidation;

namespace Quraaa.Application.Features.Profiles.Queries.GetMyProfile
{
    public class GetMyProfileQueryValidator : AbstractValidator<GetMyProfileQuery>
    {
        public GetMyProfileQueryValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User id is required.");
        }
    }
}
