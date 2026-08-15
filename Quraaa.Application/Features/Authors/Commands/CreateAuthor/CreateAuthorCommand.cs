using MediatR;
using Quraaa.Application.Features.Authors.Common;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Authors.Commands.CreateAuthor
{
    public record CreateAuthorCommand(
        string Name,
        string? Bio,
        string? PhotoUrl,
        DateTime? BirthDate
    ) : IRequest<AppResult<AuthorResponse>>;
}
