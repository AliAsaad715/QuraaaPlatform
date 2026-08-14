using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Authentication.Interfaces;
using Quraaa.Application.Features.Profiles.Common;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Profiles.Commands.SetDefaultLocation;

public sealed class SetDefaultLocationCommandHandler
    : BaseApplicationService<SetDefaultLocationCommandHandler>,
      IRequestHandler<SetDefaultLocationCommand, AppResult<LocationResponse>>
{
    private readonly IUserRepository _userRepository;

    public SetDefaultLocationCommandHandler(
        IUserRepository userRepository,
        ILogger<SetDefaultLocationCommandHandler> logger,
        IServiceProvider serviceProvider)
        : base(logger, serviceProvider)
    {
        _userRepository = userRepository;
    }

    public async Task<AppResult<LocationResponse>> Handle(
        SetDefaultLocationCommand request,
        CancellationToken cancellationToken)
    {
        return await ExecuteAsync<SetDefaultLocationCommand, LocationResponse>(request, async () =>
        {
            var user = await _userRepository.GetUserWithLocationsByIdAsync(
                request.UserId,
                cancellationToken)
                ?? throw new NotFoundException("User was not found.");

            var location = user.SetDefaultLocation(request.LocationId, request.UserId)
                ?? throw new NotFoundException("Location was not found.");

            await _userRepository.SaveChangesAsync(cancellationToken);

            return LocationResponse.FromLocation(location, user.DefaultLocationId);
        }, "Default location updated successfully");
    }
}
