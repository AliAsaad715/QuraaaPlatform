using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Application.Features.Orders.Common;
using Quraaa.Application.Features.Orders.Interfaces;
using Quraaa.Domain.Marketplace.Enums;
using Quraaa.Domain.Orders;
using Quraaa.Domain.Orders.Entities;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Orders.Commands.Common
{
    internal static class SellerOrderItemFulfillmentAccess
    {
        internal static async Task<(OrderAggregate Order, OrderItem Item)> LoadAsync(
            IOrderRepository orderRepository,
            ILibraryRepository libraryRepository,
            Guid requestingUserId,
            Guid orderId,
            Guid orderItemId,
            CancellationToken cancellationToken)
        {
            var library = await libraryRepository.GetApprovedByUserIdAsync(
                requestingUserId,
                cancellationToken);

            var order = await orderRepository.GetByIdForSellerAsync(
                orderId,
                orderItemId,
                requestingUserId,
                library?.Id,
                cancellationToken)
                ?? throw new NotFoundException("Order item not found.");

            var item = order.Items.FirstOrDefault(candidate => candidate.Id == orderItemId)
                ?? throw new NotFoundException("Order item not found.");

            var isOwnedUserListing =
                item.SellerType == SellerType.User
                && item.SellerId == requestingUserId;

            var isOwnedLibraryListing =
                item.SellerType == SellerType.Library
                && library is not null
                && item.SellerId == library.Id;

            if (!isOwnedUserListing && !isOwnedLibraryListing)
            {
                throw new UnauthorizedAccessException(
                    "You cannot fulfill another seller's order item.");
            }

            if (item.Format != ListingFormat.Physical)
            {
                throw new ConflictException(
                    "Only physical order items require seller fulfillment.");
            }

            return (order, item);
        }

        internal static SellerOrderItemFulfillmentResponse ToResponse(
            OrderAggregate order,
            OrderItem item)
        {
            return new SellerOrderItemFulfillmentResponse(
                order.Id,
                item.Id,
                order.Status,
                order.PaymentStatus,
                item.FulfillmentStatus,
                item.FulfilledAtUtc,
                order.CompletedAtUtc);
        }
    }
}
