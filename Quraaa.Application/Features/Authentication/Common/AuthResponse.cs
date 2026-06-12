namespace Quraaa.Application.Features.Authentication.Common
{
    public record AuthResponse(
        string AccessToken,
        string RefreshToken,
        DateTime AccessTokenExpiration
    );
}
