using MediatR;
using Quraaa.Application.Shared.Results;
namespace Quraaa.Application.Features.Profiles.Commands.DeleteLocation
{
    public record DeleteLocationCommand(Guid UserId, Guid LocationId) : IRequest<AppResult>;
}
