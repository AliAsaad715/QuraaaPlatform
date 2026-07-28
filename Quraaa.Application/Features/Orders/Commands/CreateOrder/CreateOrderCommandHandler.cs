using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Authentication.Interfaces;
using Quraaa.Application.Features.Carts.Interfaces;
using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Application.Features.Listings.Interfaces;
using Quraaa.Application.Features.Orders.Common;
using Quraaa.Application.Features.Orders.Interfaces;
using Quraaa.Application.Features.Orders.Services;
using Quraaa.Application.Features.Payments.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Cart.Enums;
using Quraaa.Domain.Marketplace;
using Quraaa.Domain.Marketplace.Enums;
using Quraaa.Domain.Orders;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Orders.Commands.CreateOrder
{
    public class CreateOrderCommandHandler
        : BaseApplicationService<CreateOrderCommandHandler>,
          IRequestHandler<CreateOrderCommand, AppResult<OrderCheckoutResponse>>
    {
        private readonly ICartRepository _cartRepository;
        private readonly IListingRepository _listingRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILibraryRepository _libraryRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IOrderCheckoutService _orderCheckoutService;
        private readonly IPaymentGateway _paymentGateway;

        public CreateOrderCommandHandler(
            ICartRepository cartRepository,
            IListingRepository listingRepository,
            IUserRepository userRepository,
            ILibraryRepository libraryRepository,
            IOrderRepository orderRepository,
            IOrderCheckoutService orderCheckoutService,
            IPaymentGateway paymentGateway,
            ILogger<CreateOrderCommandHandler> logger,
            IServiceProvider serviceProvider)
            : base(logger, serviceProvider)
        {
            _cartRepository = cartRepository;
            _listingRepository = listingRepository;
            _userRepository = userRepository;
            _libraryRepository = libraryRepository;
            _orderRepository = orderRepository;
            _orderCheckoutService = orderCheckoutService;
            _paymentGateway = paymentGateway;
        }

        public async Task<AppResult<OrderCheckoutResponse>> Handle(
            CreateOrderCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync(request, async () =>
            {
                var cart = await _cartRepository.GetOpenCartByUserIdAsync(
                    request.BuyerUserId,
                    cancellationToken)
                    ?? throw new NotFoundException("An active cart was not found.");

                if (cart.Status == CartStatus.PendingPayment)
                {
                    var existingOrder = cart.PendingOrderId.HasValue
                        ? await _orderRepository.GetByIdForBuyerAsync(
                            cart.PendingOrderId.Value,
                            request.BuyerUserId,
                            cancellationToken)
                        : await _orderRepository.GetActiveBySourceCartIdAsync(
                            cart.Id,
                            cancellationToken);

                    if (existingOrder is null
                        || existingOrder.BuyerUserId != request.BuyerUserId)
                    {
                        if (!cart.PendingOrderId.HasValue
                            && !string.IsNullOrWhiteSpace(
                                cart.StripeCheckoutSessionId))
                        {
                            // Cutover support for sessions created by the
                            // pre-order cart checkout. Stripe must confirm the
                            // Session is expired/open-and-expirable before the
                            // cart is unlocked; a complete Session is left for
                            // its paid webhook.
                            await _paymentGateway.ExpireCheckoutSessionAsync(
                                cart.StripeCheckoutSessionId,
                                $"expire-legacy-cart:{cart.Id:N}",
                                cancellationToken);

                            cart.ReopenAfterLegacyPaymentFailure(
                                cart.StripeCheckoutSessionId);
                            await _orderRepository.SaveChangesAsync(
                                cancellationToken);

                            throw new ConflictException(
                                "The previous checkout was closed and the cart was reopened. Retry order creation.");
                        }

                        throw new ConflictException(
                            "The cart is locked by a checkout that cannot be resumed.");
                    }

                    return await _orderCheckoutService.EnsureCheckoutSessionAsync(
                        existingOrder,
                        cart,
                        request.SuccessUrl,
                        request.CancelUrl,
                        cancellationToken);
                }

                if (!cart.Items.Any())
                {
                    throw new ConflictException("The cart is empty.");
                }

                var buyer = await _userRepository.GetUserByIdAsync(request.BuyerUserId)
                    ?? throw new NotFoundException("Buyer profile not found.");

                var buyerLibrary = await _libraryRepository.GetByUserIdAsync(
                    request.BuyerUserId,
                    cancellationToken);

                var shippingLatitude =
                    request.ShippingLatitude ?? buyer.Location?.Latitude;
                var shippingLongitude =
                    request.ShippingLongitude ?? buyer.Location?.Longitude;

                var snapshots = new List<OrderItemSnapshot>(cart.Items.Count);
                var reservableListings = new List<(ListingAggregate Listing, int Quantity)>();

                foreach (var cartItem in cart.Items)
                {
                    var listing = await _listingRepository.GetByIdAsync(
                        cartItem.ListingId,
                        cancellationToken)
                        ?? throw new NotFoundException(
                            "One or more cart listings are no longer active.");

                    if (listing.SellerType == SellerType.User
                        && listing.UserId == request.BuyerUserId)
                    {
                        throw new ConflictException("You cannot buy your own listing.");
                    }

                    if (listing.SellerType == SellerType.Library
                        && buyerLibrary is not null
                        && listing.LibraryId == buyerLibrary.Id)
                    {
                        throw new ConflictException(
                            "You cannot buy a listing owned by your library.");
                    }

                    if (listing.Format == ListingFormat.Physical
                        && (listing.Stock is null || listing.Stock < cartItem.Quantity))
                    {
                        throw new ConflictException(
                            "One or more order items exceed available stock.");
                    }

                    var details = await _listingRepository.GetByIdWithDetailsAsync(
                        listing.Id,
                        cancellationToken)
                        ?? throw new NotFoundException("Book details were not found.");

                    var (sellerId, sellerName) = await ResolveSellerAsync(
                        listing,
                        cancellationToken);

                    snapshots.Add(new OrderItemSnapshot(
                        listing.BookId,
                        listing.Id,
                        listing.SellerType,
                        sellerId,
                        listing.Format,
                        details.Book.Title,
                        details.Book.Author,
                        details.Book.CoverImageUrl,
                        sellerName,
                        listing.DigitalAssetUrl,
                        listing.Condition,
                        cartItem.Quantity,
                        ToUsdMinorUnits(listing.Price)));

                    if (listing.Format == ListingFormat.Physical)
                    {
                        reservableListings.Add((listing, cartItem.Quantity));
                    }
                }

                var order = OrderAggregate.Create(
                    request.BuyerUserId,
                    cart.Id,
                    _paymentGateway.Currency,
                    snapshots,
                    shippingLatitude,
                    shippingLongitude);

                foreach (var (listing, quantity) in reservableListings)
                {
                    listing.ReserveStock(quantity, request.BuyerUserId);
                }

                cart.BeginCheckout(order.Id);
                await _orderRepository.AddAsync(order, cancellationToken);

                return await _orderCheckoutService.EnsureCheckoutSessionAsync(
                    order,
                    cart,
                    request.SuccessUrl,
                    request.CancelUrl,
                    cancellationToken);
            }, "Order and Stripe checkout session created successfully");
        }

        private async Task<(Guid SellerId, string SellerName)> ResolveSellerAsync(
            ListingAggregate listing,
            CancellationToken cancellationToken)
        {
            if (listing.SellerType == SellerType.Library)
            {
                var libraryId = listing.LibraryId
                    ?? throw new DomainException("The library seller id is missing.");

                var library = await _libraryRepository.GetByIdAsync(
                    libraryId,
                    cancellationToken)
                    ?? throw new NotFoundException("The listing library was not found.");

                return (library.Id, library.LibraryName);
            }

            var sellerUserId = listing.UserId
                ?? throw new DomainException("The user seller id is missing.");

            var seller = await _userRepository.GetUserByIdAsync(sellerUserId)
                ?? throw new NotFoundException("The listing seller was not found.");

            return (
                seller.Id,
                $"{seller.FirstName} {seller.LastName}".Trim());
        }

        private static long ToUsdMinorUnits(decimal amount)
        {
            var scaled = decimal.Round(
                amount * 100m,
                0,
                MidpointRounding.AwayFromZero);

            return checked((long)scaled);
        }
    }
}
