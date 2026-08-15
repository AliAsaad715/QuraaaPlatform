using MediatR;
using Quraaa.Application.Features.Authors.Common;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Authors.Commands.UpdateAuthor
{
    public record UpdateAuthorCommand(
        Guid Id,
        string Name,
        string? Bio,
        string? PhotoUrl,
        DateTime? BirthDate,
        Guid ModifiedBy
    ) : IRequest<AppResult<AuthorResponse>>;
}
