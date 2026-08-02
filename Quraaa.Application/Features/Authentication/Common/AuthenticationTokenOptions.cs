namespace Quraaa.Application.Features.Authentication.Common
{
    public sealed class AuthenticationTokenOptions
    {
        public required double AccessTokenDurationInMinutes { get; init; }
    }
}
