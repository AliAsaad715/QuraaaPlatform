using MediatR;
using Quraaa.Application.Features.Authors.Common;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Authors.Queries.GetPublicAuthorDetails
{
    public sealed record GetPublicAuthorDetailsQuery(Guid AuthorId)
        : IRequest<AppResult<PublicAuthorDetailsResponse>>;
}
