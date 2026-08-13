using MediatR;
using Quraaa.Application.Features.Profiles.Common;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Profiles.Queries.GetMyLocations;

public sealed record GetMyLocationsQuery(Guid UserId)
    : IRequest<AppResult<IReadOnlyCollection<LocationResponse>>>;
