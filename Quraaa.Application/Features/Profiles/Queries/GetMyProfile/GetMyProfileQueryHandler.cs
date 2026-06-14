using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Authentication.Interfaces;
using Quraaa.Application.Features.Profiles.Common;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Profiles.Queries.GetMyProfile
{
    public class GetMyProfileQueryHandler : BaseApplicationService<GetMyProfileQueryHandler>, IRequestHandler<GetMyProfileQuery, AppResult<ProfileResponse>>
    {
        private readonly IUserRepository _userRepository;

        public GetMyProfileQueryHandler(
            IUserRepository userRepository,
            ILogger<GetMyProfileQueryHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _userRepository = userRepository;
        }

        public async Task<AppResult<ProfileResponse>> Handle(GetMyProfileQuery request, CancellationToken cancellationToken)
        {
            return await ExecuteAsync<GetMyProfileQuery, ProfileResponse>(request, async () =>
            {
                var user = await _userRepository.GetUserByIdAsync(request.UserId);
                if (user == null)
                {
                    throw new NotFoundException("User was not found.");
                }

                return ProfileResponse.FromUser(user);
            }, "Profile retrieved successfully");
        }
    }
}
