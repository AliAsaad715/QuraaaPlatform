using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Carts.Common;
using Quraaa.Application.Features.Carts.Interfaces;
using Quraaa.Application.Features.Listings.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Cart.Enums;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Carts.Commands.CreateCheckoutSession
{
    public class CreateCheckoutSessionCommandHandler : BaseApplicationService<CreateCheckoutSessionCommandHandler>, IRequestHandler<CreateCheckoutSessionCommand, AppResult<StripeCheckoutSessionResponse>>
    {
        private readonly ICartRepository _cartRepository;
        private readonly IListingRepository _listingRepository;
        private readonly IStripePaymentService _stripePaymentService;

        public CreateCheckoutSessionCommandHandler(
            ICartRepository cartRepository,
            IListingRepository listingRepository,
            IStripePaymentService stripePaymentService,
            ILogger<CreateCheckoutSessionCommandHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _cartRepository = cartRepository;
            _listingRepository = listingRepository;
            _stripePaymentService = stripePaymentService;
        }

        public async Task<AppResult<StripeCheckoutSessionResponse>> Handle(CreateCheckoutSessionCommand request, CancellationToken cancellationToken)
        {
            return await ExecuteAsync(request, async () =>
            {
                var cart = await _cartRepository.GetOpenCartByUserIdAsync(request.UserId, cancellationToken)
                    ?? throw new NotFoundException("Cart not found.");

                if (!cart.Items.Any())
                {
                    throw new DomainException("Cart is empty.");
                }

                var lineItems = new List<StripeCheckoutLineItemRequest>();

                foreach (var item in cart.Items)
                {
                    var listing = await _listingRepository.GetByIdAsync(item.ListingId, cancellationToken)
                        ?? throw new NotFoundException("Listing not found or not active.");

                    if (listing.Stock is not null && listing.Stock < item.Quantity)
                    {
                        throw new ConflictException("One or more cart items exceed available stock.");
                    }

                    var details = await _listingRepository.GetByIdWithDetailsAsync(item.ListingId, cancellationToken);

                    lineItems.Add(new StripeCheckoutLineItemRequest(
                        details?.Book.Title ?? "Book",
                        details?.Book.Author,
                        (long)Math.Round(item.UnitPriceSnapshot * 100m, MidpointRounding.AwayFromZero),
                        "usd",
                        item.Quantity));
                }

                var metadata = new Dictionary<string, string>
                {
                    ["userId"] = request.UserId.ToString(),
                    ["cartId"] = cart.Id.ToString()
                };

                var session = await _stripePaymentService.CreateCheckoutSessionAsync(
                    lineItems,
                    request.SuccessUrl,
                    request.CancelUrl,
                    metadata,
                    cancellationToken);

                cart.MarkPendingPayment(session.SessionId);
                await _cartRepository.SaveChangesAsync(cancellationToken);

                return session;
            }, "Stripe checkout session created successfully");
        }
    }
}
