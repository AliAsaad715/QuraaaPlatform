using MediatR;
using Quraaa.Application.Features.Profiles.Common;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Profiles.Commands.UpdateLocation;

public sealed record UpdateLocationCommand(
    Guid UserId,
    Guid LocationId,
    string Name,
    string? Address,
    double Latitude,
    double Longitude) : IRequest<AppResult<LocationResponse>>;
