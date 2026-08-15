using MediatR;
using Quraaa.Application.Features.Authors.Common;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Authors.Queries.GetAuthorById
{
    public record GetAuthorByIdQuery(
        Guid Id
    ) : IRequest<AppResult<AuthorDetailsResponse>>;
}
