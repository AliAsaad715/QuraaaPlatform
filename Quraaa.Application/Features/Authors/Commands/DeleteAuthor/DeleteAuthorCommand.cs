using MediatR;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Authors.Commands.DeleteAuthor
{
    public record DeleteAuthorCommand(
        Guid Id
    ) : IRequest<AppResult>;
}
