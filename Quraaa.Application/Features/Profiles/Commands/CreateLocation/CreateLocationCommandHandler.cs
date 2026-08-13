using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Authentication.Interfaces;
using Quraaa.Application.Features.Profiles.Common;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Profiles.Commands.CreateLocation;

public sealed class CreateLocationCommandHandler
    : BaseApplicationService<CreateLocationCommandHandler>,
      IRequestHandler<CreateLocationCommand, AppResult<LocationResponse>>
{
    private readonly IUserRepository _userRepository;

    public CreateLocationCommandHandler(
        IUserRepository userRepository,
        ILogger<CreateLocationCommandHandler> logger,
        IServiceProvider serviceProvider)
        : base(logger, serviceProvider)
    {
        _userRepository = userRepository;
    }

    public async Task<AppResult<LocationResponse>> Handle(
        CreateLocationCommand request,
        CancellationToken cancellationToken)
    {
        return await ExecuteAsync<CreateLocationCommand, LocationResponse>(request, async () =>
        {
            var user = await _userRepository.GetUserWithLocationsByIdAsync(
                request.UserId,
                cancellationToken)
                ?? throw new NotFoundException("User was not found.");

            var location = user.AddLocation(
                request.Name,
                request.Address,
                request.Latitude,
                request.Longitude,
                request.IsDefault,
                request.UserId);

            await _userRepository.SaveChangesAsync(cancellationToken);

            return LocationResponse.FromLocation(location, user.DefaultLocationId);
        }, "Location created successfully");
    }
}
