using MediatR;
using Quraaa.Application.Features.Ebooks.Common;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Ebooks.Queries.GetEbooks
{
    public record GetEbooksQuery(
        int PageNumber = 1,
        int PageSize = 20,
        string? SearchTerm = null
    ) : IRequest<AppResult<PagedResult<EbookResponse>>>;
}
