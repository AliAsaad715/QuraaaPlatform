using Quraaa.Domain.Shared.Entities;

namespace Quraaa.Domain.Marketplace.Events
{
    public sealed record ListingDigitalAssetUpdatedDomainEvent(
        Guid ListingId,
        Guid BookId,
        Guid LibraryId) : IDomainEvents;
}
