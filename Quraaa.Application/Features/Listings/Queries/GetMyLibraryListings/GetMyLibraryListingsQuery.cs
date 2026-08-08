using FluentValidation;
using MediatR;
using Quraaa.Application.Features.Listings.Queries.GetLibraryBooks;
using Quraaa.Application.Shared.Requests;
using Quraaa.Application.Shared.Results;
using Quraaa.Domain.Marketplace.Enums;

namespace Quraaa.Application.Features.Listings.Queries.GetMyLibraryListings
{
    public record GetMyLibraryListingsQuery(
        Guid UserId,
        string? SearchTerm,
        string? SortBy,
        bool SortDescending,
        ListingStatus? Status = null) : PaginationRequestDTO, IRequest<AppResult<PagedResult<ListingSummaryResponse>>>;

    public sealed class GetMyLibraryListingsQueryValidator : AbstractValidator<GetMyLibraryListingsQuery>
    {
        private static readonly IReadOnlySet<string> AllowedSortFields =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "title", "author", "quantity" };

        public GetMyLibraryListingsQueryValidator()
        {
            RuleFor(x => x.SortBy)
                .Must(s => s is null || AllowedSortFields.Contains(s))
                .WithMessage($"SortBy must be one of: {string.Join(", ", AllowedSortFields)}.");
        }
    }
}