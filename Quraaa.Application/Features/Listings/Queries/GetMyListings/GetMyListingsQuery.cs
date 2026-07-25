using FluentValidation;
using MediatR;
using Quraaa.Application.Features.Listings.Queries.GetLibraryBooks;
using Quraaa.Application.Shared.Requests;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Listings.Queries.GetMyListings
{
    public record GetMyListingsQuery(
        Guid UserId,
        string? SearchTerm,
        string? SortBy,
        bool SortDescending) : PaginationRequestDTO, IRequest<AppResult<PagedResult<ListingSummaryResponse>>>;

    public sealed class GetMyListingsQueryValidator : AbstractValidator<GetMyListingsQuery>
    {
        private static readonly IReadOnlySet<string> AllowedSortFields =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "title", "author", "quantity" };

        public GetMyListingsQueryValidator()
        {
            RuleFor(x => x.SortBy)
                .Must(s => s is null || AllowedSortFields.Contains(s))
                .WithMessage($"SortBy must be one of: {string.Join(", ", AllowedSortFields)}.");
        }
    }
}