using Quraaa.Domain.Shared.Entities;

namespace Quraaa.Domain.Marketplace.Events
{
    public sealed record LibraryListingPublishedDomainEvent(
        Guid ListingId,
        Guid BookId,
        Guid LibraryId) : IDomainEvents;
}
