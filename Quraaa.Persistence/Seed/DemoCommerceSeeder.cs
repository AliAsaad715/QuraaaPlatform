using Microsoft.EntityFrameworkCore;
using Quraaa.Domain.Cart;
using Quraaa.Domain.Cart.Enums;
using Quraaa.Domain.Library;
using Quraaa.Domain.Marketplace;
using Quraaa.Domain.Marketplace.Enums;
using Quraaa.Domain.Orders;
using Quraaa.Domain.Orders.Entities;
using Quraaa.Domain.Orders.Enums;
using Quraaa.Domain.Payouts;
using Quraaa.Domain.Purchases;
using Quraaa.Domain.Shared.Entities;
using Quraaa.Domain.User;
using Quraaa.Persistence.Data;

namespace Quraaa.Persistence.Seed;

public static class DemoCommerceSeeder
{
    private enum SeedOrderState
    {
        Completed,
        Processing,
        Pending,
        Failed,
        Cancelled,
        Expired,
    }

    private sealed record OrderLine(Guid ListingId, int Quantity = 1);

    private sealed record ListingDetails(
        ListingAggregate Listing,
        Guid BookId,
        string BookTitle,
        string BookAuthor,
        string? BookCoverImageUrl,
        Guid SellerId,
        string SellerName);

    public static async Task SeedAsync(
        ApplicationDbContext context,
        CancellationToken cancellationToken = default)
    {
        await EnsureOrderAsync(
            context,
            DemoSeedData.CheckoutMarkers.CompletedDigital,
            DemoSeedData.OrderNumbers.CompletedDigital,
            DemoSeedData.MainBuyerPhoneNumber,
            [new(DemoSeedData.Listings.DuneDigital)],
            SeedOrderState.Completed,
            daysAgo: 30,
            cancellationToken: cancellationToken);

        await EnsureOrderAsync(
            context,
            DemoSeedData.CheckoutMarkers.CompletedMixed,
            DemoSeedData.OrderNumbers.CompletedMixed,
            DemoSeedData.MainBuyerPhoneNumber,
            [
                new(DemoSeedData.Listings.GranadaLibrary),
                new(DemoSeedData.Listings.CleanCodeLibraryTwo),
                new(DemoSeedData.Listings.DuneDigital),
            ],
            SeedOrderState.Completed,
            daysAgo: 14,
            cancellationToken: cancellationToken);

        await EnsureOrderAsync(
            context,
            DemoSeedData.CheckoutMarkers.ProcessingUserSale,
            DemoSeedData.OrderNumbers.ProcessingUserSale,
            DemoSeedData.ReporterOnePhoneNumber,
            [new(DemoSeedData.Listings.PragmaticProgrammerUser)],
            SeedOrderState.Processing,
            daysAgo: 2,
            cancellationToken: cancellationToken);

        await EnsureOrderAsync(
            context,
            DemoSeedData.CheckoutMarkers.PendingCheckout,
            DemoSeedData.OrderNumbers.PendingCheckout,
            DemoSeedData.CheckoutBuyerPhoneNumber,
            [new(DemoSeedData.Listings.DunePhysical)],
            SeedOrderState.Pending,
            daysAgo: 0,
            cancellationToken: cancellationToken);

        await EnsureOrderAsync(
            context,
            DemoSeedData.CheckoutMarkers.FailedPayment,
            DemoSeedData.OrderNumbers.FailedPayment,
            DemoSeedData.ReporterTwoPhoneNumber,
            [new(DemoSeedData.Listings.GranadaLibrary)],
            SeedOrderState.Failed,
            daysAgo: 5,
            cancellationToken: cancellationToken);

        await EnsureOrderAsync(
            context,
            DemoSeedData.CheckoutMarkers.CancelledOrder,
            DemoSeedData.OrderNumbers.CancelledOrder,
            DemoSeedData.ReporterThreePhoneNumber,
            [new(DemoSeedData.Listings.CleanCodeLibraryOne)],
            SeedOrderState.Cancelled,
            daysAgo: 7,
            cancellationToken: cancellationToken);

        await EnsureOrderAsync(
            context,
            DemoSeedData.CheckoutMarkers.ExpiredOrder,
            DemoSeedData.OrderNumbers.ExpiredOrder,
            DemoSeedData.SellerPhoneNumber,
            [new(DemoSeedData.Listings.GranadaLibrary)],
            SeedOrderState.Expired,
            daysAgo: 10,
            cancellationToken: cancellationToken);

        await EnsureMainBuyerCartAsync(context, cancellationToken);
    }

    private static async Task EnsureOrderAsync(
        ApplicationDbContext context,
        string checkoutSessionId,
        string orderNumber,
        string buyerPhoneNumber,
        IReadOnlyCollection<OrderLine> lines,
        SeedOrderState targetState,
        int daysAgo,
        CancellationToken cancellationToken)
    {
        var existingOrder = await context.Orders
            .Include(order => order.Items)
            .Include(order => order.PaymentAttempts)
            .FirstOrDefaultAsync(
                order =>
                    order.OrderNumber == orderNumber ||
                    order.PaymentAttempts.Any(
                        attempt => attempt.CheckoutSessionId == checkoutSessionId),
                cancellationToken);

        if (existingOrder is not null)
        {
            if (existingOrder.PaymentStatus == PaymentStatus.Paid)
            {
                await EnsurePurchasesAsync(context, existingOrder, cancellationToken);
                await EnsurePayoutsAsync(
                    context,
                    existingOrder,
                    checkoutSessionId,
                    cancellationToken);
            }

            return;
        }

        var buyerId = await context.UsersProfiles
            .Where(user => user.PhoneNumber == buyerPhoneNumber)
            .Select(user => (Guid?)user.Id)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException(
                $"Demo buyer {buyerPhoneNumber} must be seeded first.");

        var listingDetails = await LoadListingDetailsAsync(
            context,
            lines.Select(line => line.ListingId),
            cancellationToken);

        var snapshots = lines.Select(line =>
        {
            var details = listingDetails[line.ListingId];
            var listing = details.Listing;

            return new OrderItemSnapshot(
                details.BookId,
                listing.Id,
                listing.SellerType,
                details.SellerId,
                listing.Format,
                details.BookTitle,
                details.BookAuthor,
                details.BookCoverImageUrl,
                details.SellerName,
                listing.CustomDigitalAssetUrl,
                listing.Condition,
                line.Quantity,
                ToMinorUnits(listing.Price));
        }).ToArray();

        var cart = CartAggregate.Create(buyerId);
        foreach (var line in lines)
        {
            var listing = listingDetails[line.ListingId].Listing;
            cart.AddOrIncreaseItem(listing.Id, line.Quantity, listing.Price);

            if (listing.Format == ListingFormat.Physical)
            {
                listing.ReserveStock(line.Quantity, buyerId);
            }
        }

        var hasPhysicalItems = snapshots.Any(snapshot => snapshot.Format == ListingFormat.Physical);
        var order = OrderAggregate.Create(
            buyerId,
            cart.Id,
            "usd",
            snapshots,
            shippingLatitude: hasPhysicalItems ? 33.3152 : null,
            shippingLongitude: hasPhysicalItems ? 44.3661 : null);

        cart.BeginCheckout(order.Id);
        PaymentAttempt? attempt = null;
        if (targetState != SeedOrderState.Pending)
        {
            attempt = order.StartPaymentAttempt(
                PaymentProvider.Stripe,
                "https://demo.quraaa.local/payment/success",
                "https://demo.quraaa.local/payment/cancel",
                DateTime.UtcNow.AddDays(30));
            order.AttachCheckoutSession(
                attempt.Id,
                checkoutSessionId,
                $"https://checkout.stripe.com/c/pay/{checkoutSessionId}",
                DateTime.UtcNow.AddDays(30));
            cart.AttachCheckoutSession(order.Id, checkoutSessionId);
        }

        switch (targetState)
        {
            case SeedOrderState.Completed:
                order.MarkPaid(attempt!.Id, ToPaymentIntentId(checkoutSessionId));
                cart.MarkPaid(order.Id, ToPaymentIntentId(checkoutSessionId));
                foreach (var physicalItem in order.Items.Where(
                             item => item.Format == ListingFormat.Physical))
                {
                    order.MarkItemFulfilled(physicalItem.Id, physicalItem.SellerId);
                }
                break;

            case SeedOrderState.Processing:
                order.MarkPaid(attempt!.Id, ToPaymentIntentId(checkoutSessionId));
                cart.MarkPaid(order.Id, ToPaymentIntentId(checkoutSessionId));
                var processingItem = order.Items.First(
                    item => item.Format == ListingFormat.Physical);
                order.MarkItemProcessing(processingItem.Id, processingItem.SellerId);
                break;

            case SeedOrderState.Pending:
                break;

            case SeedOrderState.Failed:
                order.MarkPaymentFailed(
                    attempt!.Id,
                    "card_declined",
                    "Demo card was declined; the cart was reopened for retry.");
                cart.ReopenAfterPaymentFailure(order.Id);
                ReleaseStock(listingDetails, lines, buyerId);
                break;

            case SeedOrderState.Cancelled:
                order.Cancel(buyerId, "Buyer cancelled this demo checkout.");
                cart.ReopenAfterPaymentFailure(order.Id);
                ReleaseStock(listingDetails, lines, buyerId);
                break;

            case SeedOrderState.Expired:
                order.MarkExpired(attempt!.Id);
                cart.ReopenAfterPaymentFailure(order.Id);
                ReleaseStock(listingDetails, lines, buyerId);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(targetState), targetState, null);
        }

        await context.Carts.AddAsync(cart, cancellationToken);
        await context.Orders.AddAsync(order, cancellationToken);

        var historicalTime = DateTime.UtcNow.AddDays(-daysAgo);
        context.Entry(order)
            .Property(nameof(OrderAggregate.OrderNumber))
            .CurrentValue = orderNumber;

        if (order.PaymentStatus == PaymentStatus.Paid)
        {
            await AddMissingPurchasesAsync(context, order, historicalTime, cancellationToken);
            await AddMissingPayoutsAsync(
                context,
                order,
                checkoutSessionId,
                historicalTime,
                cancellationToken);
        }

        ApplyHistoricalTimeline(
            context,
            cart,
            order,
            targetState,
            historicalTime);

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task<Dictionary<Guid, ListingDetails>> LoadListingDetailsAsync(
        ApplicationDbContext context,
        IEnumerable<Guid> listingIds,
        CancellationToken cancellationToken)
    {
        var ids = listingIds.Distinct().ToArray();
        var listings = await context.Listings
            .Where(listing => ids.Contains(listing.Id))
            .ToListAsync(cancellationToken);

        if (listings.Count != ids.Length)
        {
            throw new InvalidOperationException("One or more demo commerce listings are missing.");
        }

        var bookIds = listings.Select(listing => listing.BookId).Distinct().ToArray();
        var books = await context.Books
            .IgnoreQueryFilters()
            .Where(book => bookIds.Contains(book.Id))
            .ToListAsync(cancellationToken);
        var bookById = books.ToDictionary(book => book.Id);

        var authorIds = books
            .Where(book => book.AuthorId.HasValue)
            .Select(book => book.AuthorId!.Value)
            .Distinct()
            .ToArray();
        var authorNames = await context.Authors
            .Where(author => authorIds.Contains(author.Id))
            .ToDictionaryAsync(
                author => author.Id,
                author => author.Name,
                cancellationToken);

        var libraryIds = listings
            .Where(listing => listing.LibraryId.HasValue)
            .Select(listing => listing.LibraryId!.Value)
            .Distinct()
            .ToArray();
        var libraries = await context.Libraries
            .Where(library => libraryIds.Contains(library.Id))
            .ToDictionaryAsync(library => library.Id, cancellationToken);

        var userIds = listings
            .Where(listing => listing.UserId.HasValue)
            .Select(listing => listing.UserId!.Value)
            .Distinct()
            .ToArray();
        var users = await context.UsersProfiles
            .Where(user => userIds.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id, cancellationToken);

        var result = new Dictionary<Guid, ListingDetails>();
        foreach (var listing in listings)
        {
            var book = bookById[listing.BookId];
            var author = book.AuthorId.HasValue &&
                         authorNames.TryGetValue(book.AuthorId.Value, out var name)
                ? name
                : "Unknown author";

            var sellerId = listing.LibraryId ?? listing.UserId
                ?? throw new InvalidOperationException("Demo listing has no seller.");
            var sellerName = listing.LibraryId.HasValue
                ? libraries[listing.LibraryId.Value].LibraryName
                : $"{users[listing.UserId!.Value].FirstName} {users[listing.UserId.Value].LastName}";

            result[listing.Id] = new ListingDetails(
                listing,
                book.Id,
                book.Title,
                author,
                book.CoverImageUrl,
                sellerId,
                sellerName);
        }

        return result;
    }

    private static void ReleaseStock(
        IReadOnlyDictionary<Guid, ListingDetails> listingDetails,
        IEnumerable<OrderLine> lines,
        Guid modifiedBy)
    {
        foreach (var line in lines)
        {
            var listing = listingDetails[line.ListingId].Listing;
            if (listing.Format == ListingFormat.Physical)
            {
                listing.ReleaseReservedStock(line.Quantity, modifiedBy);
            }
        }
    }

    private static async Task EnsurePurchasesAsync(
        ApplicationDbContext context,
        OrderAggregate order,
        CancellationToken cancellationToken)
    {
        await AddMissingPurchasesAsync(
            context,
            order,
            order.CreationTime,
            cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task AddMissingPurchasesAsync(
        ApplicationDbContext context,
        OrderAggregate order,
        DateTime creationTime,
        CancellationToken cancellationToken)
    {
        var itemIds = order.Items.Select(item => item.Id).ToArray();
        var existingItemIds = await context.BookPurchases
            .Where(purchase =>
                purchase.OrderItemId.HasValue &&
                itemIds.Contains(purchase.OrderItemId.Value))
            .Select(purchase => purchase.OrderItemId!.Value)
            .ToHashSetAsync(cancellationToken);

        foreach (var item in order.Items.Where(item => !existingItemIds.Contains(item.Id)))
        {
            var purchase = BookPurchaseAggregate.Create(
                order.BuyerUserId,
                item.BookId,
                item.ListingId,
                item.Quantity,
                item.UnitPriceMinor / 100m,
                order.Id,
                item.Id,
                item.DigitalAssetUrlSnapshot);

            await context.BookPurchases.AddAsync(purchase, cancellationToken);
            SetCreationTime(context, purchase, creationTime);
        }
    }

    private static async Task EnsurePayoutsAsync(
        ApplicationDbContext context,
        OrderAggregate order,
        string checkoutSessionId,
        CancellationToken cancellationToken)
    {
        await AddMissingPayoutsAsync(
            context,
            order,
            checkoutSessionId,
            order.CreationTime,
            cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task AddMissingPayoutsAsync(
        ApplicationDbContext context,
        OrderAggregate order,
        string checkoutSessionId,
        DateTime creationTime,
        CancellationToken cancellationToken)
    {
        if (checkoutSessionId is not (
            DemoSeedData.CheckoutMarkers.CompletedDigital or
            DemoSeedData.CheckoutMarkers.CompletedMixed))
        {
            return;
        }

        var libraryGroups = order.Items
            .Where(item => item.SellerType == SellerType.Library)
            .GroupBy(item => item.SellerId)
            .ToArray();
        var libraryIds = libraryGroups.Select(group => group.Key).ToArray();

        var existingLibraryIds = await context.SellerPayouts
            .Where(payout => payout.OrderId == order.Id)
            .Select(payout => payout.LibraryId)
            .ToHashSetAsync(cancellationToken);

        var libraries = await context.Libraries
            .Where(library => libraryIds.Contains(library.Id))
            .ToDictionaryAsync(library => library.Id, cancellationToken);

        foreach (var group in libraryGroups.Where(group => !existingLibraryIds.Contains(group.Key)))
        {
            var library = libraries[group.Key];
            var grossAmountMinor = group.Sum(item => item.TotalPriceMinor);
            var share = checkoutSessionId == DemoSeedData.CheckoutMarkers.CompletedMixed &&
                        string.Equals(library.Email, "info.lib2@quraaa.com", StringComparison.OrdinalIgnoreCase)
                ? 0m
                : library.ProfitSharePercent;

            var payout = SellerPayoutAggregate.Create(
                order.Id,
                library.Id,
                order.Currency,
                grossAmountMinor,
                share,
                ToPaymentIntentId(checkoutSessionId));

            if (checkoutSessionId == DemoSeedData.CheckoutMarkers.CompletedDigital)
            {
                payout.MarkPaid(
                    "tr_demo_completed_digital_v1",
                    library.StripeConnectAccountId ?? "acct_demo_library_active_0001");
            }
            else if (payout.NetAmountMinor > 0)
            {
                payout.RecordDefinitiveRejection(
                    "Demo transfer rejected to show the manual-review payout state.",
                    maxAttempts: 1);
            }

            await context.SellerPayouts.AddAsync(payout, cancellationToken);
            SetCreationTime(context, payout, creationTime);
        }
    }

    private static async Task EnsureMainBuyerCartAsync(
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var buyerId = await context.UsersProfiles
            .Where(user => user.PhoneNumber == DemoSeedData.MainBuyerPhoneNumber)
            .Select(user => (Guid?)user.Id)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("The main demo buyer is missing.");

        var cart = await context.Carts
            .Include(candidate => candidate.Items)
            .Where(candidate =>
                candidate.UserId == buyerId &&
                !candidate.IsDeleted &&
                (candidate.Status == CartStatus.Active ||
                 candidate.Status == CartStatus.PendingPayment))
            .OrderByDescending(candidate => candidate.CreationTime)
            .FirstOrDefaultAsync(cancellationToken);

        if (cart?.Status == CartStatus.PendingPayment)
        {
            return;
        }

        if (cart is not null && cart.Items.Count > 0)
        {
            return;
        }

        if (cart is null)
        {
            cart = CartAggregate.Create(buyerId);
            await context.Carts.AddAsync(cart, cancellationToken);
        }

        var listingIds = new[]
        {
            DemoSeedData.Listings.CleanCodeLibraryOne,
            DemoSeedData.Listings.GranadaLibrary,
        };
        var listings = await context.Listings
            .Where(listing =>
                listingIds.Contains(listing.Id) &&
                listing.Status == ListingStatus.Active &&
                (listing.Format == ListingFormat.Digital || listing.Stock > 0))
            .ToListAsync(cancellationToken);

        foreach (var listing in listings.Where(
                     listing => cart.Items.All(item => item.ListingId != listing.Id)))
        {
            cart.AddOrIncreaseItem(listing.Id, 1, listing.Price);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static long ToMinorUnits(decimal amount) => checked((long)decimal.Round(
        amount * 100m,
        0,
        MidpointRounding.AwayFromZero));

    private static string ToPaymentIntentId(string checkoutSessionId) =>
        checkoutSessionId.Replace("cs_", "pi_", StringComparison.Ordinal);

    private static void ApplyHistoricalTimeline(
        ApplicationDbContext context,
        CartAggregate cart,
        OrderAggregate order,
        SeedOrderState targetState,
        DateTime creationTime)
    {
        var transitionTime = creationTime.AddMinutes(30);
        var completionTime = transitionTime.AddMinutes(45);

        SetCreationTime(context, cart, creationTime);
        SetCreationTime(context, order, creationTime);
        context.Entry(cart)
            .Property(nameof(AuditableEntity.LastModificationTime))
            .CurrentValue = transitionTime;
        context.Entry(order)
            .Property(nameof(AuditableEntity.LastModificationTime))
            .CurrentValue = completionTime;

        foreach (var attempt in order.PaymentAttempts)
        {
            context.Entry(attempt)
                .Property(nameof(PaymentAttempt.ExpiresAtUtc))
                .CurrentValue = transitionTime.AddHours(1);

            if (attempt.Status is not (
                    PaymentAttemptStatus.Succeeded or
                    PaymentAttemptStatus.Failed or
                    PaymentAttemptStatus.Cancelled or
                    PaymentAttemptStatus.Expired))
            {
                continue;
            }

            context.Entry(attempt)
                .Property(nameof(PaymentAttempt.CompletedAtUtc))
                .CurrentValue = transitionTime;
        }

        if (targetState is SeedOrderState.Completed or SeedOrderState.Processing)
        {
            context.Entry(order)
                .Property(nameof(OrderAggregate.PaidAtUtc))
                .CurrentValue = transitionTime;
        }

        if (targetState == SeedOrderState.Completed)
        {
            context.Entry(order)
                .Property(nameof(OrderAggregate.CompletedAtUtc))
                .CurrentValue = completionTime;
        }
        else if (targetState == SeedOrderState.Cancelled)
        {
            context.Entry(order)
                .Property(nameof(OrderAggregate.CancelledAtUtc))
                .CurrentValue = transitionTime;
        }
        else if (targetState == SeedOrderState.Expired)
        {
            context.Entry(order)
                .Property(nameof(OrderAggregate.ExpiredAtUtc))
                .CurrentValue = transitionTime;
        }

        foreach (var item in order.Items)
        {
            if (item.FulfillmentStatus == OrderItemFulfillmentStatus.Fulfilled)
            {
                context.Entry(item)
                    .Property(nameof(OrderItem.FulfilledAtUtc))
                    .CurrentValue = completionTime;
            }
            else if (item.FulfillmentStatus == OrderItemFulfillmentStatus.Cancelled)
            {
                context.Entry(item)
                    .Property(nameof(OrderItem.CancelledAtUtc))
                    .CurrentValue = transitionTime;
            }
        }

        foreach (var payout in context.ChangeTracker
                     .Entries<SellerPayoutAggregate>()
                     .Select(entry => entry.Entity)
                     .Where(payout => payout.OrderId == order.Id))
        {
            SetCreationTime(context, payout, completionTime);
            context.Entry(payout)
                .Property(nameof(AuditableEntity.LastModificationTime))
                .CurrentValue = completionTime.AddMinutes(5);
            context.Entry(payout)
                .Property(nameof(SellerPayoutAggregate.NextAttemptAtUtc))
                .CurrentValue = completionTime.AddMinutes(5);

            if (payout.LastAttemptAtUtc.HasValue)
            {
                context.Entry(payout)
                    .Property(nameof(SellerPayoutAggregate.LastAttemptAtUtc))
                    .CurrentValue = completionTime.AddMinutes(5);
            }

            if (payout.PaidAtUtc.HasValue)
            {
                context.Entry(payout)
                    .Property(nameof(SellerPayoutAggregate.PaidAtUtc))
                    .CurrentValue = completionTime.AddMinutes(5);
            }
        }
    }

    private static void SetCreationTime(
        ApplicationDbContext context,
        AuditableEntity entity,
        DateTime creationTime)
    {
        context.Entry(entity)
            .Property(nameof(AuditableEntity.CreationTime))
            .CurrentValue = creationTime;
    }
}
