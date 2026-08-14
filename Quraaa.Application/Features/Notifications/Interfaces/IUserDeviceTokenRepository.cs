namespace Quraaa.Application.Features.Notifications.Interfaces
{
    public interface IUserDeviceTokenRepository
    {
        /// <summary>
        /// Registers or refreshes a device token for a user. A token is unique across the
        /// whole table — if it already belongs to a different user (e.g. a shared/reset
        /// device), ownership is reassigned to the new caller.
        /// </summary>
        Task UpsertAsync(
            Guid userId,
            string deviceToken,
            DateTime nowUtc,
            CancellationToken cancellationToken = default);

        /// <summary>Distinct device tokens registered by any of the given users, for FCM fan-out.</summary>
        Task<HashSet<string>> GetTokensByUserIdsAsync(
            IReadOnlyCollection<Guid> userIds,
            CancellationToken cancellationToken = default);

        /// <summary>Removes tokens FCM has reported as no longer valid.</summary>
        Task RemoveTokensAsync(
            IReadOnlyCollection<string> deviceTokens,
            CancellationToken cancellationToken = default);

        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
