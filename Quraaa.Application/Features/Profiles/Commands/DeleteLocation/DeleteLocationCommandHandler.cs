using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Authentication.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Profiles.Commands.DeleteLocation
{
    public class DeleteLocationCommandHandler : BaseApplicationService<DeleteLocationCommandHandler>, IRequestHandler<DeleteLocationCommand, AppResult>
    {
        private readonly IUserRepository _userRepository;

        public DeleteLocationCommandHandler(
            IUserRepository userRepository,
            ILogger<DeleteLocationCommandHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _userRepository = userRepository;
        }

        public async Task<AppResult> Handle(DeleteLocationCommand request, CancellationToken cancellationToken)
        {
            return await ExecuteAsync(request, async () =>
            {
                var user = await _userRepository.GetUserWithLocationsByIdAsync(
                    request.UserId,
                    cancellationToken)
                    ?? throw new NotFoundException("User was not found.");

                if (!user.RemoveLocation(request.LocationId, request.UserId))
                {
                    throw new NotFoundException("Location was not found.");
                }

                await _userRepository.SaveChangesAsync(cancellationToken);
            }, "Location deleted successfully");
        }
    }
}
