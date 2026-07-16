using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Carts.Common;
using Quraaa.Application.Features.Carts.Interfaces;
using Quraaa.Application.Features.Listings.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Cart;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Carts.Commands.AddCartItem
{
    public class AddCartItemCommandHandler : BaseApplicationService<AddCartItemCommandHandler>, IRequestHandler<AddCartItemCommand, AppResult<CartResponse>>
    {
        private readonly ICartRepository _cartRepository;
        private readonly IListingRepository _listingRepository;

        public AddCartItemCommandHandler(
            ICartRepository cartRepository,
            IListingRepository listingRepository,
            ILogger<AddCartItemCommandHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _cartRepository = cartRepository;
            _listingRepository = listingRepository;
        }

        public async Task<AppResult<CartResponse>> Handle(AddCartItemCommand request, CancellationToken cancellationToken)
        {
            return await ExecuteAsync(request, async () =>
            {
                var listing = await _listingRepository.GetByIdAsync(request.ListingId, cancellationToken);
                if (listing is null)
                {
                    throw new NotFoundException("Listing not found or not active.");
                }

                if (listing.Stock is not null && listing.Stock < request.Quantity)
                {
                    throw new ConflictException("Requested quantity exceeds available stock.");
                }

                var cart = await _cartRepository.GetOpenCartByUserIdAsync(request.UserId, cancellationToken);
                var isNewCart = cart is null;
                cart ??= CartAggregate.Create(request.UserId);

                cart.AddOrIncreaseItem(request.ListingId, request.Quantity, listing.Price);

                if (isNewCart)
                {
                    await _cartRepository.AddAsync(cart, cancellationToken);
                }

                await _cartRepository.SaveChangesAsync(cancellationToken);

                return CartResponse.FromCart(cart);
            }, "Cart item added successfully");
        }
    }
}
