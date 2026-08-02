namespace Quraaa.Application.Features.Authentication.Interfaces
{
    public interface IAccessTokenRevocationService
    {
        Task RevokeAsync(
            string tokenId,
            DateTimeOffset expiresAt,
            CancellationToken cancellationToken = default);

        Task<bool> IsRevokedAsync(
            string tokenId,
            CancellationToken cancellationToken = default);
    }
}
