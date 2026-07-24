using MediatR;
using Quraaa.Application.Shared.Results;
using System.Text.Json.Serialization;

namespace Quraaa.Application.Features.Profiles.Commands.CreateLocation
{
    public record UpsertLocationCommand(
        [property: JsonIgnore] Guid UserId,
        double Latitude,
        double Longitude) : IRequest<AppResult>;
}
