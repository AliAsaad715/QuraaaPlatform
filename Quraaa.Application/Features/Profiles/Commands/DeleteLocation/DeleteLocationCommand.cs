using MediatR;
using Quraaa.Application.Shared.Results;
using System.Text.Json.Serialization;

namespace Quraaa.Application.Features.Profiles.Commands.DeleteLocation
{
    public record DeleteLocationCommand([property: JsonIgnore] Guid UserId) : IRequest<AppResult>;
}
