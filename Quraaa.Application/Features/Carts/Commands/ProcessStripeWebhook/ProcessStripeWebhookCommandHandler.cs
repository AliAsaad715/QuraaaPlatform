using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Carts.Interfaces;
using Quraaa.Application.Features.Listings.Interfaces;
using Quraaa.Application.Features.Purchases.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Cart.Enums;
using Quraaa.Domain.Marketplace.Enums;
using Quraaa.Domain.Purchases;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Carts.Commands.ProcessStripeWebhook
{
    public class ProcessStripeWebhookCommandHandler : BaseApplicationService<ProcessStripeWebhookCommandHandler>, IRequestHandler<ProcessStripeWebhookCommand, AppResult>
    {
        private readonly IStripePaymentService _stripePaymentService;
        private readonly ICartRepository _cartRepository;
        private readonly IBookPurchaseRepository _purchaseRepository;
        private readonly IListingRepository _listingRepository;

        public ProcessStripeWebhookCommandHandler(
            IStripePaymentService stripePaymentService,
            ICartRepository cartRepository,
            IBookPurchaseRepository purchaseRepository,
            IListingRepository listingRepository,
            ILogger<ProcessStripeWebhookCommandHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _stripePaymentService = stripePaymentService;
            _cartRepository = cartRepository;
            _purchaseRepository = purchaseRepository;
            _listingRepository = listingRepository;
        }

        public async Task<AppResult> Handle(ProcessStripeWebhookCommand request, CancellationToken cancellationToken)
        {
            return await ExecuteAsync(request, async () =>
            {
                var webhook = await _stripePaymentService.ParseWebhookEventAsync(
                    request.Payload,
                    request.SignatureHeader,
                    cancellationToken);

                if (!string.Equals(webhook.EventType, "checkout.session.completed", StringComparison.Ordinal))
                {
                    return;
                }

                var cart = await _cartRepository.GetByStripeSessionIdAsync(webhook.SessionId, cancellationToken)
                    ?? throw new NotFoundException("Cart not found for checkout session.");

                if (cart.Status == CartStatus.Paid)
                {
                    return;
                }

                var purchases = new List<BookPurchaseAggregate>();

                foreach (var item in cart.Items)
                {
                    var listing = await _listingRepository.GetByIdAsync(item.ListingId, cancellationToken)
                        ?? throw new NotFoundException("Listing not found.");

                    purchases.Add(BookPurchaseAggregate.Create(
                        cart.UserId,
                        listing.BookId,
                        listing.Id,
                        item.Quantity,
                        item.UnitPriceSnapshot));

                    if (listing.Format == ListingFormat.Physical)
                    {
                        for (var i = 0; i < item.Quantity; i++)
                        {
                            listing.DecrementStock(cart.UserId);
                        }
                    }
                }

                await _purchaseRepository.AddRangeAsync(purchases, cancellationToken);
                cart.MarkPaid(webhook.PaymentIntentId);

                await _cartRepository.SaveChangesAsync(cancellationToken);
            }, "Stripe webhook processed successfully");
        }
    }
}
