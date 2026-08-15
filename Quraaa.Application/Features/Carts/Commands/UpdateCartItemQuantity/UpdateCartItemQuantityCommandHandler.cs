using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Carts.Common;
using Quraaa.Application.Features.Carts.Interfaces;
using Quraaa.Application.Features.Listings.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Marketplace.Enums;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Carts.Commands.UpdateCartItemQuantity
{
    public class UpdateCartItemQuantityCommandHandler : BaseApplicationService<UpdateCartItemQuantityCommandHandler>, IRequestHandler<UpdateCartItemQuantityCommand, AppResult<CartResponse>>
    {
        private readonly ICartRepository _cartRepository;
        private readonly IListingRepository _listingRepository;

        public UpdateCartItemQuantityCommandHandler(
            ICartRepository cartRepository,
            IListingRepository listingRepository,
            ILogger<UpdateCartItemQuantityCommandHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _cartRepository = cartRepository;
            _listingRepository = listingRepository;
        }

        public async Task<AppResult<CartResponse>> Handle(UpdateCartItemQuantityCommand request, CancellationToken cancellationToken)
        {
            return await ExecuteAsync(request, async () =>
            {
                var cart = await _cartRepository.GetOpenCartByUserIdAsync(request.UserId, cancellationToken)
                    ?? throw new NotFoundException("No open cart was found for the current user.");

                if (!cart.Items.Any(item => item.ListingId == request.ListingId))
                {
                    throw new NotFoundException("The listing is not in the current user's cart.");
                }

                // Inventory lookup distinguishes a missing id from an existing listing that
                // became unavailable after it was added to the cart.
                var listing = await _listingRepository.GetByIdForInventoryAsync(
                        request.ListingId,
                        cancellationToken)
                    ?? throw new NotFoundException("Listing not found.");

                if (listing.Status != ListingStatus.Active)
                {
                    throw new ConflictException(
                        $"The listing is no longer available (status: {listing.Status}). Remove it from the cart.");
                }

                if (listing.Stock is not null && listing.Stock < request.Quantity)
                {
                    throw new ConflictException("Requested quantity exceeds available stock.");
                }

                cart.UpdateItemQuantity(request.ListingId, request.Quantity);

                await _cartRepository.SaveChangesAsync(cancellationToken);

                return CartResponse.FromCart(cart);
            }, "Cart item updated successfully");
        }
    }
}
