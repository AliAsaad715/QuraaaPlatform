using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Authentication.Interfaces;
using Quraaa.Application.Features.Profiles.Common;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Profiles.Queries.GetMyLocations;

public sealed class GetMyLocationsQueryHandler
    : BaseApplicationService<GetMyLocationsQueryHandler>,
      IRequestHandler<GetMyLocationsQuery, AppResult<IReadOnlyCollection<LocationResponse>>>
{
    private readonly IUserRepository _userRepository;

    public GetMyLocationsQueryHandler(
        IUserRepository userRepository,
        ILogger<GetMyLocationsQueryHandler> logger,
        IServiceProvider serviceProvider)
        : base(logger, serviceProvider)
    {
        _userRepository = userRepository;
    }

    public async Task<AppResult<IReadOnlyCollection<LocationResponse>>> Handle(
        GetMyLocationsQuery request,
        CancellationToken cancellationToken)
    {
        return await ExecuteAsync<GetMyLocationsQuery, IReadOnlyCollection<LocationResponse>>(
            request,
            async () =>
            {
                var user = await _userRepository.GetUserWithLocationsByIdAsync(
                    request.UserId,
                    cancellationToken)
                    ?? throw new NotFoundException("User was not found.");

                return user.Locations
                    .OrderByDescending(location => location.Id == user.DefaultLocationId)
                    .ThenBy(location => location.CreationTime)
                    .ThenBy(location => location.Id)
                    .Select(location => LocationResponse.FromLocation(
                        location,
                        user.DefaultLocationId))
                    .ToArray();
            },
            "Locations retrieved successfully");
    }
}
