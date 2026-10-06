using Quraaa.Domain.Shared.Events;

namespace Quraaa.Domain.Marketplace.Events
{
    public sealed record ListingDigitalAssetUpdatedDomainEvent(
        Guid ListingId,
        Guid BookId,
        Guid LibraryId) : DomainEvent;
}
