using MediatR;
using Quraaa.Application.Features.Profiles.Common;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Profiles.Commands.SetDefaultLocation;

public sealed record SetDefaultLocationCommand(
    Guid UserId,
    Guid LocationId) : IRequest<AppResult<LocationResponse>>;
