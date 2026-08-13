using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Authentication.Interfaces;
using Quraaa.Application.Features.Carts.Interfaces;
using Quraaa.Application.Features.Listings.Interfaces;
using Quraaa.Application.Features.Orders.Common;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Cart.Enums;
using Quraaa.Domain.Marketplace.Enums;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Orders.Queries.GetOrderCheckoutContext;

public sealed class GetOrderCheckoutContextQueryHandler
    : BaseApplicationService<GetOrderCheckoutContextQueryHandler>,
      IRequestHandler<GetOrderCheckoutContextQuery, AppResult<OrderCheckoutContextResponse>>
{
    private readonly ICartRepository _cartRepository;
    private readonly IListingRepository _listingRepository;
    private readonly IUserRepository _userRepository;

    public GetOrderCheckoutContextQueryHandler(
        ICartRepository cartRepository,
        IListingRepository listingRepository,
        IUserRepository userRepository,
        ILogger<GetOrderCheckoutContextQueryHandler> logger,
        IServiceProvider serviceProvider)
        : base(logger, serviceProvider)
    {
        _cartRepository = cartRepository;
        _listingRepository = listingRepository;
        _userRepository = userRepository;
    }

    public async Task<AppResult<OrderCheckoutContextResponse>> Handle(
        GetOrderCheckoutContextQuery request,
        CancellationToken cancellationToken)
    {
        return await ExecuteAsync<GetOrderCheckoutContextQuery, OrderCheckoutContextResponse>(
            request,
            async () =>
            {
                var cart = await _cartRepository.GetOpenCartByUserIdAsync(
                    request.BuyerUserId,
                    cancellationToken);

                if (cart is null)
                {
                    return OrderCheckoutContextResponse.WithoutShipping();
                }

                if (cart.Status == CartStatus.PendingPayment)
                {
                    throw new ConflictException(
                        "The cart is locked by a pending order. Resume or cancel that order before starting another checkout.");
                }

                if (cart.Items.Count == 0)
                {
                    return OrderCheckoutContextResponse.WithoutShipping();
                }

                var listingIds = cart.Items
                    .Select(item => item.ListingId)
                    .Distinct()
                    .ToArray();
                var formats = await _listingRepository.GetActiveFormatsByIdsAsync(
                    listingIds,
                    cancellationToken);

                if (formats.Count != listingIds.Length)
                {
                    throw new NotFoundException(
                        "One or more cart listings are no longer active.");
                }

                if (!formats.Values.Any(format => format == ListingFormat.Physical))
                {
                    return OrderCheckoutContextResponse.WithoutShipping();
                }

                var buyer = await _userRepository.GetUserWithLocationsByIdAsync(
                    request.BuyerUserId,
                    cancellationToken)
                    ?? throw new NotFoundException("Buyer profile not found.");

                var selectedLocationId = buyer.DefaultLocation is null
                    ? null
                    : buyer.DefaultLocationId;
                var locations = buyer.Locations
                    .OrderByDescending(location => location.Id == selectedLocationId)
                    .ThenBy(location => location.CreationTime)
                    .ThenBy(location => location.Id)
                    .Select(location => new CheckoutLocationResponse(
                        location.Id,
                        location.Name,
                        location.Address,
                        location.Latitude,
                        location.Longitude,
                        location.Id == selectedLocationId))
                    .ToArray();

                return new OrderCheckoutContextResponse(
                    true,
                    selectedLocationId,
                    locations);
            },
            "Order checkout context retrieved successfully");
    }
}
