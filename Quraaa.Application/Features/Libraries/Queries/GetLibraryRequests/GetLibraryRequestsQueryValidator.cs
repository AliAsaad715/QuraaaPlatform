using FluentValidation;

namespace Quraaa.Application.Features.Libraries.Queries.GetLibraryRequests
{
    public sealed class GetLibraryRequestsQueryValidator : AbstractValidator<GetLibraryRequestsQuery>
    {
        public GetLibraryRequestsQueryValidator()
        {
            RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);

            RuleFor(x => x.Status)
                .IsInEnum()
                .When(x => x.Status.HasValue);
        }
    }
}