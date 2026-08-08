using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Listings.Interfaces;
using Quraaa.Application.Features.Purchases.Common;
using Quraaa.Application.Features.Purchases.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Marketplace.Enums;
using Quraaa.Domain.Purchases;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Purchases.Commands.PurchaseDigitalBook
{
    public sealed class PurchaseDigitalBookCommandHandler
        : BaseApplicationService<PurchaseDigitalBookCommandHandler>,
          IRequestHandler<PurchaseDigitalBookCommand, AppResult<PurchaseDigitalBookResponse>>
    {
        private readonly IListingRepository _listingRepository;
        private readonly IBookPurchaseRepository _purchaseRepository;

        public PurchaseDigitalBookCommandHandler(
            IListingRepository listingRepository,
            IBookPurchaseRepository purchaseRepository,
            ILogger<PurchaseDigitalBookCommandHandler> logger,
            IServiceProvider serviceProvider)
            : base(logger, serviceProvider)
        {
            _listingRepository = listingRepository;
            _purchaseRepository = purchaseRepository;
        }

        public Task<AppResult<PurchaseDigitalBookResponse>> Handle(
            PurchaseDigitalBookCommand command,
            CancellationToken cancellationToken) =>
            ExecuteAsync<PurchaseDigitalBookCommand, PurchaseDigitalBookResponse>(
                command,
                () => ProcessAsync(command, cancellationToken),
                "Digital book purchased successfully.");

        private async Task<PurchaseDigitalBookResponse> ProcessAsync(
            PurchaseDigitalBookCommand command,
            CancellationToken cancellationToken)
        {
            var listing = await _listingRepository.GetByIdAsync(command.ListingId, cancellationToken)
                ?? throw new NotFoundException("Listing not found.");

            if (listing.Format != ListingFormat.Digital)
                throw new DomainException("Only digital listings can be purchased through this endpoint.");

            if (listing.Status != ListingStatus.Active)
                throw new ConflictException("This listing is not currently available for purchase.");

            // A user cannot buy their own listing.
            if (listing.UserId == command.RequestingUserId ||
                listing.LibraryId is not null && listing.SellerType == SellerType.Library)
            {
                // Library listings are sold by a library, not a user — skip the seller check.
                // For user listings, block self-purchase.
                if (listing.SellerType == SellerType.User &&
                    listing.UserId == command.RequestingUserId)
                    throw new DomainException("You cannot purchase your own listing.");
            }

            if (await _purchaseRepository.HasUserPurchasedListingAsync(
                    command.RequestingUserId, listing.Id, cancellationToken))
                throw new ConflictException("You have already purchased this digital book.");

            // KEY INVARIANT: snapshot the asset URL NOW, before any seller can
            // replace the file. This URL is immutable on the purchase record —
            // ListingAggregate.UpdateCustomDigitalAsset() only affects future buyers.
            var assetUrl = listing.CustomDigitalAssetUrl
                ?? throw new DomainException("This digital listing has no asset attached.");

            var purchase = BookPurchaseAggregate.Create(
                userId:                    command.RequestingUserId,
                bookId:                    listing.BookId,
                listingId:                 listing.Id,
                quantity:                  1,
                unitPrice:                 listing.Price,
                purchasedDigitalAssetUrl:  assetUrl);

            await _purchaseRepository.AddRangeAsync([purchase], cancellationToken);
            await _purchaseRepository.SaveChangesAsync(cancellationToken);

            return new PurchaseDigitalBookResponse(
                purchase.Id,
                listing.BookId,
                purchase.PurchasedDigitalAssetUrl!,
                purchase.UnitPrice,
                purchase.CreationTime);
        }
    }
}
