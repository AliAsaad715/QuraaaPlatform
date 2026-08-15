namespace Quraaa.Application.Features.Payouts.Common
{
    /// <summary>
    /// Provider-neutral view of a Stripe connected account used to validate a
    /// library owner's wallet before storing or paying it.
    /// </summary>
    /// <param name="OwnerLibraryId">
    /// The library this account was created for by the platform (from the
    /// account metadata written at creation), or null for accounts the
    /// platform did not create.
    /// </param>
    public sealed record PayoutConnectedAccountStatus(
        bool TransfersCapabilityActive,
        Guid? OwnerLibraryId = null)
    {
        /// <summary>
        /// True when the platform created this account for the given library,
        /// or when the account carries no platform ownership metadata at all
        /// (an account attached by id that the platform did not create).
        /// False when it was created for a DIFFERENT library.
        /// </summary>
        public bool BelongsToLibraryOrIsUnowned(Guid libraryId) =>
            OwnerLibraryId is null || OwnerLibraryId == libraryId;

        // Stripe requires the destination account's "transfers" capability to
        // be active for it to receive a Transfer; payouts_enabled alone does
        // not guarantee that.
        public bool CanReceiveTransfers => TransfersCapabilityActive;
    }
}
