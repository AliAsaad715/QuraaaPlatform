using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Application.Features.Authentication.Interfaces;

namespace Quraaa.Application.Features.Profiles.Commands.CreateLocation
{
    public class UpsertLocationCommandHandler : BaseApplicationService<UpsertLocationCommandHandler>, IRequestHandler<UpsertLocationCommand, AppResult>
    {
        private readonly IUserRepository _userRepository;

        public UpsertLocationCommandHandler(
            IUserRepository userRepository,
            ILogger<UpsertLocationCommandHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _userRepository = userRepository;
        }

        public async Task<AppResult> Handle(UpsertLocationCommand request, CancellationToken cancellationToken)
        {
            return await ExecuteAsync(request, async () =>
            {
                var user = await _userRepository.GetUserByIdAsync(request.UserId);
                user!.SetLocation(request.Latitude, request.Longitude);
                await _userRepository.SaveChangesAsync();

            }, "Location upserted successfully");
        }
    }
}
