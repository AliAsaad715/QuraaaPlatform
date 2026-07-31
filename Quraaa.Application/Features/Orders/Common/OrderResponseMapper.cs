using Quraaa.Domain.Marketplace.Enums;
using Quraaa.Domain.Orders;
using Quraaa.Domain.Orders.Entities;
using Quraaa.Domain.Orders.Enums;

namespace Quraaa.Application.Features.Orders.Common
{
    public static class OrderResponseMapper
    {
        private const decimal MinorUnitsPerUsd = 100m;

        public static OrderResponse ToResponse(this OrderAggregate order)
        {
            var items = order.Items
                .OrderBy(item => item.Id)
                .Select(item => new OrderItemResponse(
                    item.Id,
                    item.ListingId,
                    item.BookId,
                    item.SellerType,
                    item.SellerId,
                    item.SellerNameSnapshot,
                    item.Format,
                    item.ConditionSnapshot,
                    item.BookTitleSnapshot,
                    item.BookAuthorSnapshot,
                    item.BookCoverImageUrlSnapshot,
                    item.Quantity,
                    ToMajorUnits(item.UnitPriceMinor),
                    ToMajorUnits(item.TotalPriceMinor),
                    item.FulfillmentStatus,
                    order.PaymentStatus == PaymentStatus.Paid
                        && item.Format == ListingFormat.Digital
                        && !string.IsNullOrWhiteSpace(item.DigitalAssetUrlSnapshot)))
                .ToList();

            var shippingLocation =
                order.ShippingLatitude.HasValue && order.ShippingLongitude.HasValue
                    ? new ShippingLocationResponse(
                        order.ShippingLatitude.Value,
                        order.ShippingLongitude.Value)
                    : null;

            return new OrderResponse(
                order.Id,
                order.OrderNumber,
                order.Status,
                order.PaymentStatus,
                order.Currency,
                ToMajorUnits(order.SubtotalAmountMinor),
                ToMajorUnits(order.ShippingAmountMinor),
                ToMajorUnits(order.DiscountAmountMinor),
                ToMajorUnits(order.TotalAmountMinor),
                items.Sum(item => item.Quantity),
                shippingLocation,
                items,
                order.CreationTime,
                order.PaidAtUtc,
                order.CancelledAtUtc,
                order.CancellationReason);
        }

        public static OrderSummaryResponse ToSummaryResponse(this OrderAggregate order)
        {
            return new OrderSummaryResponse(
                order.Id,
                order.OrderNumber,
                order.Status,
                order.PaymentStatus,
                order.Currency,
                ToMajorUnits(order.TotalAmountMinor),
                order.Items.Sum(item => item.Quantity),
                order.CreationTime,
                order.PaidAtUtc);
        }

        public static OrderCheckoutResponse ToCheckoutResponse(
            this OrderAggregate order,
            PaymentAttempt attempt)
        {
            if (string.IsNullOrWhiteSpace(attempt.CheckoutSessionId)
                || string.IsNullOrWhiteSpace(attempt.CheckoutUrl)
                || !attempt.ExpiresAtUtc.HasValue)
            {
                throw new InvalidOperationException(
                    "The payment attempt does not have an attached checkout session.");
            }

            return new OrderCheckoutResponse(
                order.ToResponse(),
                attempt.Id,
                attempt.CheckoutSessionId,
                attempt.CheckoutUrl,
                new DateTimeOffset(
                    DateTime.SpecifyKind(attempt.ExpiresAtUtc.Value, DateTimeKind.Utc)));
        }

        public static decimal ToMajorUnits(long amountMinor)
        {
            return amountMinor / MinorUnitsPerUsd;
        }
    }
}
