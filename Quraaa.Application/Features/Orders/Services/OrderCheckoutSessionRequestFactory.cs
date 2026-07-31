using Quraaa.Application.Features.Payments.Common;
using Quraaa.Domain.Orders;
using Quraaa.Domain.Orders.Entities;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Orders.Services
{
    internal static class OrderCheckoutSessionRequestFactory
    {
        public static readonly TimeSpan CheckoutLifetime = TimeSpan.FromHours(1);

        public static PaymentCheckoutSessionRequest Create(
            OrderAggregate order,
            PaymentAttempt attempt)
        {
            var expiresAtUtc = attempt.ExpiresAtUtc
                ?? throw new ConflictException(
                    "The payment attempt does not have an expiry.");

            var metadata = new Dictionary<string, string>
            {
                ["orderId"] = order.Id.ToString(),
                ["paymentAttemptId"] = attempt.Id.ToString(),
                ["buyerUserId"] = order.BuyerUserId.ToString(),
                ["cartId"] = order.SourceCartId.ToString()
            };

            return new PaymentCheckoutSessionRequest(
                order.Items
                    .OrderBy(item => item.Id)
                    .Select(item => new PaymentCheckoutLineItem(
                        item.BookTitleSnapshot,
                        $"{item.BookAuthorSnapshot} · {item.SellerNameSnapshot}",
                        item.UnitPriceMinor,
                        item.Quantity))
                    .ToList(),
                order.Id.ToString(),
                metadata,
                $"checkout:{attempt.Id:N}",
                attempt.SuccessUrl,
                attempt.CancelUrl,
                new DateTimeOffset(
                    DateTime.SpecifyKind(expiresAtUtc, DateTimeKind.Utc)));
        }
    }
}
