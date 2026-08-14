using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Authentication.Interfaces;
using Quraaa.Application.Features.Categories.Interfaces;
using Quraaa.Application.Features.Profiles.Common;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Profiles.Commands.UpdateProfile
{
    public class UpdateProfileCommandHandler : BaseApplicationService<UpdateProfileCommandHandler>, IRequestHandler<UpdateProfileCommand, AppResult<ProfileResponse>>
    {
        private readonly IUserRepository _userRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IImageUrlFormatter _imageUrlFormatter;

        public UpdateProfileCommandHandler(
            IUserRepository userRepository,
            ICategoryRepository categoryRepository,
            IImageUrlFormatter imageUrlFormatter,
            ILogger<UpdateProfileCommandHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _userRepository = userRepository;
            _categoryRepository = categoryRepository;
            _imageUrlFormatter = imageUrlFormatter;
        }

        public async Task<AppResult<ProfileResponse>> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
        {
            return await ExecuteAsync<UpdateProfileCommand, ProfileResponse>(request, async () =>
            {
                var user = await _userRepository.GetUserWithProfileDetailsByIdAsync(
                    request.UserId,
                    cancellationToken);
                if (user == null)
                {
                    throw new NotFoundException("User was not found.");
                }

                user.UpdateProfile(
                    request.FirstName,
                    request.LastName,
                    request.Gender,
                    request.DateOfBirth,
                    request.ProfileImageUrl,
                    request.Interests,
                    request.UserId);

                await _userRepository.SaveChangesAsync();

                var interestCategories = await _categoryRepository.GetByIdsAsync(user.InterestedCategoryIds.ToList(), cancellationToken);

                return ProfileResponse.FromUser(user, interestCategories, _imageUrlFormatter);
            }, "Profile updated successfully");
        }
    }
}
