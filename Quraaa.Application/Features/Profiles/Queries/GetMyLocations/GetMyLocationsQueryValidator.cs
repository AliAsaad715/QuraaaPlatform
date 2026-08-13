using FluentValidation;

namespace Quraaa.Application.Features.Profiles.Queries.GetMyLocations;

public sealed class GetMyLocationsQueryValidator : AbstractValidator<GetMyLocationsQuery>
{
    public GetMyLocationsQueryValidator()
    {
        RuleFor(query => query.UserId).NotEmpty();
    }
}
