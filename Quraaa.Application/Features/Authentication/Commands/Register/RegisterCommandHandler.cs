using MediatR;
using Quraaa.Application.Features.Authentication.Common;
using Quraaa.Application.Features.Authentication.Interfaces;
using Quraaa.Domain.User;
using Quraaa.Domain.User.Enums;

namespace Quraaa.Application.Features.Authentication.Commands.Register
{
    public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponse>
    {
        private readonly IIdentityService _identityService;
        private readonly IUserRepository _userRepository;

        public RegisterCommandHandler(IIdentityService identityService, IUserRepository userRepository)
        {
            _identityService = identityService;
            _userRepository = userRepository;
        }

        public async Task<AuthResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
        {
            var isUnique = await _identityService.IsPhoneNumberUniqueAsync(request.PhoneNumber);
            if (!isUnique)
                throw new Exception("Phone number is already registered.");

            var id = Guid.NewGuid();

            var passwordHash = await _identityService.CreateUserIdentityAsync(id, request.PhoneNumber, request.Password);

            var userProfile = new UserAggregate(
                id,
                request.FirstName,
                request.LastName,
                request.PhoneNumber,
                passwordHash,
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

            return new AuthResponse(id, "Token", "Token", DateTime.Now);
        }
    }
}
