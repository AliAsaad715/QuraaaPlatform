using IdentityServer.Helpers;
using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Authentication.Common;
using Quraaa.Application.Features.Authentication.Interfaces;
using Quraaa.Application.Shared.Exceptions;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.User;
using Quraaa.Domain.User.Enums;

namespace Quraaa.Application.Features.Authentication.Commands.Register
{
    public class RegisterCommandHandler : BaseApplicationService<RegisterCommandHandler>, IRequestHandler<RegisterCommand, AppResult<AuthResponse>>
    {
        private readonly IIdentityService _identityService;
        private readonly IUserRepository _userRepository;
        private readonly IPhoneService _phoneService;

        public RegisterCommandHandler(
            IIdentityService identityService,
            IUserRepository userRepository,
            IPhoneService phoneService,
            ILogger<RegisterCommandHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _identityService = identityService;
            _userRepository = userRepository;
            _phoneService = phoneService;
        }

        public async Task<AppResult<AuthResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
        {
            return await ExecuteAsync(request, async () =>
            {
                var isUnique = await _identityService.IsPhoneNumberUniqueAsync(request.PhoneNumber);
                if (!isUnique)
                {
                    throw new ApplicationBusinessException("Phone number is already registered.");
                }

                var id = Guid.NewGuid();

                var identityResult = await _identityService.CreateUserIdentityAsync(id, request.PhoneNumber, request.Password);
                if (!identityResult.Succeeded)
                {
                    var allErrors = string.Join(" | ", identityResult.Errors);
                    throw new ApplicationBusinessException(allErrors);
                }

                var formattedPhone = _phoneService.FormatToE164(request.PhoneNumber) ?? request.PhoneNumber;
                var userProfile = new UserAggregate(
                    id,
                    request.FirstName,
                    request.LastName,
                    formattedPhone,
                    identityResult.PasswordHash!,
                    request.Gender,
                    Role.User,
                    request.DateOfBirth
                );

                if (request.Interests != null && request.Interests.Any())
                {
                    foreach (var interest in request.Interests)
                    {
                        userProfile.AddInterest(interest);
                    }
                }

                await _userRepository.AddUserAsync(userProfile);
                await _userRepository.SaveChangesAsync();
                var authResponse = await _identityService.GenerateAuthTokensAsync(id, request.PhoneNumber);
                return authResponse;
            }, "User registered successfully");
        }
    }
}
