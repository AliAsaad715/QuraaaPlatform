using Quraaa.Domain.Shared.Events;

namespace Quraaa.Domain.Marketplace.Events
{
    public sealed record LibraryListingPublishedDomainEvent(
        Guid ListingId,
        Guid BookId,
        Guid LibraryId) : DomainEvent;
}
