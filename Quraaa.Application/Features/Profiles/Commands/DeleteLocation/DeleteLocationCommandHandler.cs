using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Authentication.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;

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
                var user = await _userRepository.GetUserByIdAsync(request.UserId);

                user!.ClearLocation();

                await _userRepository.SaveChangesAsync();
            }, "Location deleted successfully");
        }
    }
}
