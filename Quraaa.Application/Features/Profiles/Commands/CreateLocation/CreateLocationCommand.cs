using MediatR;
using Quraaa.Application.Features.Profiles.Common;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Profiles.Commands.CreateLocation;

public sealed record CreateLocationCommand(
    Guid UserId,
    string Name,
    string? Address,
    double Latitude,
    double Longitude,
    bool IsDefault) : IRequest<AppResult<LocationResponse>>;
