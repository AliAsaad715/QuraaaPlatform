using FluentValidation;
using MediatR;
using Quraaa.Application.Features.Libraries.Queries.GetLibraryBooks;
using Quraaa.Application.Shared.Requests;
using Quraaa.Application.Shared.Results;

public record GetLibraryBooksQuery : PaginationRequestDTO, IRequest<AppResult<PagedResult<LibraryBookResponse>>>
{
    public Guid LibraryId { get; init; }
    public string? SearchTerm { get; init; }
    public string? SortBy { get; init; }
    public bool SortDescending { get; init; }
}

public sealed class GetLibraryBooksQueryValidator : AbstractValidator<GetLibraryBooksQuery>
{
    private static readonly IReadOnlySet<string> AllowedSortFields =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "title", "author", "quantity" };

    public GetLibraryBooksQueryValidator()
    {
        RuleFor(x => x.SortBy)
            .Must(s => s is null || AllowedSortFields.Contains(s))
            .WithMessage($"SortBy must be one of: {string.Join(", ", AllowedSortFields)}.");
    }
}