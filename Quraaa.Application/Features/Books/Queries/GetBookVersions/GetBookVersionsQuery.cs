using MediatR;
using Quraaa.Application.Features.Books.Common;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Books.Queries.GetBookVersions
{
    /// <summary>Administrator query: a book's full change history, newest first.</summary>
    public record GetBookVersionsQuery(Guid BookId)
        : IRequest<AppResult<IReadOnlyCollection<BookVersionResponse>>>;
}
