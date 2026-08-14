using Quraaa.Application.Features.Payouts.Common;
using Quraaa.Application.Features.Payouts.Exceptions;
using Quraaa.Application.Features.Payouts.Interfaces;
using Stripe;
using System.Net;

namespace Quraaa.Infrastructure.Services
{
    /// <summary>
    /// Stripe Connect implementation of <see cref="IPayoutGateway"/>: verifies
    /// connected accounts and moves seller profit shares with the separate
    /// charges-and-transfers pattern. Shares the platform's singleton
    /// <see cref="StripeClient"/>, so all calls run in the configured
    /// test/live mode.
    /// </summary>
    public sealed class StripePayoutGateway : IPayoutGateway
    {
        private readonly AccountService _accountService;
        private readonly TransferService _transferService;

        public StripePayoutGateway(StripeClient stripeClient)
        {
            _accountService = new AccountService(stripeClient);
            _transferService = new TransferService(stripeClient);
        }

        public async Task<PayoutConnectedAccountStatus?> GetConnectedAccountAsync(
            string stripeAccountId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(stripeAccountId))
            {
                throw new ArgumentException(
                    "Stripe account id is required.",
                    nameof(stripeAccountId));
            }

            Account account;

            try
            {
                account = await _accountService.GetAsync(
                    stripeAccountId.Trim(),
                    cancellationToken: cancellationToken);
            }
            catch (StripeException exception)
                when (exception.HttpStatusCode
                        is HttpStatusCode.NotFound
                        or HttpStatusCode.Forbidden
                    || string.Equals(
                        exception.StripeError?.Code,
                        "resource_missing",
                        StringComparison.Ordinal)
                    || string.Equals(
                        exception.StripeError?.Code,
                        "account_invalid",
                        StringComparison.Ordinal))
            {
                // Unknown account, or an account that is not connected to this
                // platform. Both mean "not a usable wallet", not an outage.
                return null;
            }
            catch (StripeException exception)
            {
                throw new PayoutGatewayException(
                    "Stripe could not verify the connected account.",
                    exception);
            }

            return new PayoutConnectedAccountStatus(
                account.Id,
                account.PayoutsEnabled,
                string.Equals(
                    account.Capabilities?.Transfers,
                    "active",
                    StringComparison.OrdinalIgnoreCase),
                account.DetailsSubmitted);
        }

        public async Task<PayoutTransferResult> CreateTransferAsync(
            PayoutTransferRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (string.IsNullOrWhiteSpace(request.DestinationAccountId))
            {
                throw new ArgumentException(
                    "A destination Stripe account id is required.",
                    nameof(request));
            }

            if (request.AmountMinor <= 0)
            {
                throw new ArgumentException(
                    "Transfer amount must be positive.",
                    nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.Currency))
            {
                throw new ArgumentException(
                    "Transfer currency is required.",
                    nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
            {
                throw new ArgumentException(
                    "Transfer idempotency key is required.",
                    nameof(request));
            }

            var options = new TransferCreateOptions
            {
                Amount = request.AmountMinor,
                Currency = request.Currency.Trim().ToLowerInvariant(),
                Destination = request.DestinationAccountId.Trim(),
                TransferGroup = request.TransferGroup,
                Metadata = request.Metadata.ToDictionary(
                    pair => pair.Key,
                    pair => pair.Value),
            };

            var requestOptions = new RequestOptions
            {
                IdempotencyKey = request.IdempotencyKey,
            };

            Transfer transfer;

            try
            {
                transfer = await _transferService.CreateAsync(
                    options,
                    requestOptions,
                    cancellationToken);
            }
            catch (StripeException exception)
                when (string.Equals(
                        exception.StripeError?.Type,
                        "idempotency_error",
                        StringComparison.Ordinal)
                    && exception.HttpStatusCode != HttpStatusCode.Conflict)
            {
                // The 400 flavor: this key was already used with DIFFERENT
                // parameters (e.g. the destination wallet changed while an
                // earlier attempt was unresolved). Waiting never fixes it —
                // the caller must reconcile against Stripe.
                throw new PayoutIdempotencyKeyReuseException(
                    "The transfer idempotency key was already used with different parameters.",
                    exception);
            }
            catch (StripeException exception)
                when (exception.HttpStatusCode == HttpStatusCode.Conflict)
            {
                // The 409 flavor: another request with this idempotency key
                // is still in flight — a competing process is executing this
                // very attempt.
                throw new PayoutConcurrentAttemptException(
                    "Another transfer request for this payout is already in progress.",
                    exception);
            }
            catch (StripeException exception)
            {
                // A 4xx with a Stripe error body is a definitive rejection: no
                // transfer was created and a fresh idempotency key may safely
                // re-evaluate. Everything else (timeouts, transport errors,
                // 5xx) is indeterminate: the transfer may exist, so the retry
                // must replay the SAME key.
                var isDefinitiveRejection =
                    exception.StripeError is not null
                    && (int)exception.HttpStatusCode is >= 400 and < 500;

                throw new PayoutGatewayException(
                    exception.StripeError?.Message
                        ?? "Stripe rejected the transfer request.",
                    exception,
                    isDefinitiveRejection,
                    exception.StripeError?.Code);
            }

            return new PayoutTransferResult(transfer.Id);
        }

        public async Task<PayoutExistingTransfer?> FindTransferForPayoutAsync(
            string transferGroup,
            Guid sellerPayoutId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(transferGroup))
            {
                throw new ArgumentException(
                    "A transfer group is required.",
                    nameof(transferGroup));
            }

            var listOptions = new TransferListOptions
            {
                TransferGroup = transferGroup,
                Limit = 100,
            };

            StripeList<Transfer> transfers;

            try
            {
                transfers = await _transferService.ListAsync(
                    listOptions,
                    cancellationToken: cancellationToken);
            }
            catch (StripeException exception)
            {
                throw new PayoutGatewayException(
                    "Stripe could not list transfers for the payout.",
                    exception);
            }

            var expectedPayoutId = sellerPayoutId.ToString();

            var match = transfers.Data.FirstOrDefault(transfer =>
                transfer.Metadata is not null
                && transfer.Metadata.TryGetValue("sellerPayoutId", out var payoutId)
                && string.Equals(payoutId, expectedPayoutId, StringComparison.OrdinalIgnoreCase));

            return match is null
                ? null
                : new PayoutExistingTransfer(match.Id, match.DestinationId);
        }
    }
}
