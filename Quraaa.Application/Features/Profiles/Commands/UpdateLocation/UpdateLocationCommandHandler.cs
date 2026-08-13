using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Authentication.Interfaces;
using Quraaa.Application.Features.Profiles.Common;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Profiles.Commands.UpdateLocation;

public sealed class UpdateLocationCommandHandler
    : BaseApplicationService<UpdateLocationCommandHandler>,
      IRequestHandler<UpdateLocationCommand, AppResult<LocationResponse>>
{
    private readonly IUserRepository _userRepository;

    public UpdateLocationCommandHandler(
        IUserRepository userRepository,
        ILogger<UpdateLocationCommandHandler> logger,
        IServiceProvider serviceProvider)
        : base(logger, serviceProvider)
    {
        _userRepository = userRepository;
    }

    public async Task<AppResult<LocationResponse>> Handle(
        UpdateLocationCommand request,
        CancellationToken cancellationToken)
    {
        return await ExecuteAsync<UpdateLocationCommand, LocationResponse>(request, async () =>
        {
            var user = await _userRepository.GetUserWithLocationsByIdAsync(
                request.UserId,
                cancellationToken)
                ?? throw new NotFoundException("User was not found.");

            var location = user.UpdateLocation(
                request.LocationId,
                request.Name,
                request.Address,
                request.Latitude,
                request.Longitude,
                request.UserId)
                ?? throw new NotFoundException("Location was not found.");

            await _userRepository.SaveChangesAsync(cancellationToken);

            return LocationResponse.FromLocation(location, user.DefaultLocationId);
        }, "Location updated successfully");
    }
}
