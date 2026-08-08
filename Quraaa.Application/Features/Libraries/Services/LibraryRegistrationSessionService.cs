using Quraaa.Application.Features.Authentication.Interfaces;
using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Application.Shared.Exceptions;
using Quraaa.Domain.Library;

namespace Quraaa.Application.Features.Libraries.Services
{
    public sealed class LibraryRegistrationSessionService
    {
        private readonly ILibraryRegistrationRepository _registrationRepository;
        private readonly ILibraryRegistrationTokenService _tokenService;
        private readonly IIdentityService _identityService;

        public LibraryRegistrationSessionService(
            ILibraryRegistrationRepository registrationRepository,
            ILibraryRegistrationTokenService tokenService,
            IIdentityService identityService)
        {
            _registrationRepository = registrationRepository;
            _tokenService = tokenService;
            _identityService = identityService;
        }

        public async Task<LibraryRegistrationSession> ResolveActiveAsync(
            string rawToken,
            bool requireSubmitted,
            CancellationToken cancellationToken,
            bool allowCompleted = false)
        {
            if (!_tokenService.TryHash(rawToken, out var tokenHash))
            {
                throw new UnauthenticatedException();
            }

            var session = await _registrationRepository.GetSessionByTokenHashAsync(
                tokenHash,
                cancellationToken);
            var utcNow = DateTime.UtcNow;

            if (session is null
                || session.ExpiresAtUtc <= utcNow
                || (!allowCompleted && session.CompletedAtUtc.HasValue)
                || (requireSubmitted && !session.SubmittedAtUtc.HasValue)
                || !await _identityService.IsRefreshTokenFamilyActiveAsync(
                    session.UserId,
                    session.AuthenticationSessionId,
                    cancellationToken))
            {
                throw new UnauthenticatedException();
            }

            return session;
        }
    }
}
