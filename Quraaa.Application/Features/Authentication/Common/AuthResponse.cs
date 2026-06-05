namespace Quraaa.Application.Features.Authentication.Common
{
    public record AuthResponse(
        Guid UserId,
        string AccessToken,
        string RefreshToken,
        DateTime AccessTokenExpiration
    );
}
