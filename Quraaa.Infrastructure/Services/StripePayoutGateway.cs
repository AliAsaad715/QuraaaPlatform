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
        private readonly PaymentIntentService _paymentIntentService;
        private readonly AccountService _accountService;
        private readonly AccountLinkService _accountLinkService;
        private readonly AccountLoginLinkService _accountLoginLinkService;
        private readonly TransferService _transferService;

        public StripePayoutGateway(StripeClient stripeClient)
        {
            _paymentIntentService = new PaymentIntentService(stripeClient);
            _accountService = new AccountService(stripeClient);
            _accountLinkService = new AccountLinkService(stripeClient);
            _accountLoginLinkService = new AccountLoginLinkService(stripeClient);
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
                when (exception.HttpStatusCode == HttpStatusCode.NotFound
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
                //
                // A bare 403 is deliberately NOT treated this way: a restricted
                // or under-permissioned API key answers 403 for EVERY account,
                // and callers act on a null by detaching or deactivating the
                // wallet — one key misconfiguration would wipe every library's
                // wallet. Without an account-level error code it is an outage.
                return null;
            }
            catch (StripeException exception)
            {
                throw ToGatewayException(exception, "Stripe could not verify the connected account.");
            }
            catch (Exception exception) when (IsTransportFailure(exception, cancellationToken))
            {
                throw ToTransportException(exception);
            }

            Guid? ownerLibraryId = null;

            if (account.Metadata is not null
                && account.Metadata.TryGetValue("libraryId", out var libraryIdValue)
                && Guid.TryParse(libraryIdValue, out var parsedLibraryId))
            {
                ownerLibraryId = parsedLibraryId;
            }

            return new PayoutConnectedAccountStatus(
                string.Equals(
                    account.Capabilities?.Transfers,
                    "active",
                    StringComparison.OrdinalIgnoreCase),
                ownerLibraryId);
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
                Metadata = new Dictionary<string, string>(request.Metadata),
            };

            if (!string.IsNullOrWhiteSpace(request.SourceTransactionId))
            {
                // Draw on the order's own charge instead of the platform's
                // available balance, so the transfer is accepted while the
                // charge is still settling.
                options.SourceTransaction = request.SourceTransactionId.Trim();
            }

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
                throw ToGatewayException(exception, "Stripe rejected the transfer request.");
            }
            catch (Exception exception) when (IsTransportFailure(exception, cancellationToken))
            {
                throw ToTransportException(exception);
            }

            return new PayoutTransferResult(transfer.Id);
        }

        public async Task<string?> ResolveChargeIdAsync(
            string paymentIntentId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(paymentIntentId))
            {
                throw new ArgumentException(
                    "Payment intent id is required.",
                    nameof(paymentIntentId));
            }

            try
            {
                var paymentIntent = await _paymentIntentService.GetAsync(
                    paymentIntentId.Trim(),
                    cancellationToken: cancellationToken);

                return string.IsNullOrWhiteSpace(paymentIntent.LatestChargeId)
                    ? null
                    : paymentIntent.LatestChargeId;
            }
            catch (StripeException exception)
            {
                throw ToGatewayException(exception, "Stripe could not resolve the payment charge.");
            }
            catch (Exception exception) when (IsTransportFailure(exception, cancellationToken))
            {
                throw ToTransportException(exception);
            }
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
                throw ToGatewayException(exception, "Stripe could not list transfers for the payout.");
            }
            catch (Exception exception) when (IsTransportFailure(exception, cancellationToken))
            {
                throw ToTransportException(exception);
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

        public async Task<string> CreateConnectedAccountAsync(
            PayoutConnectedAccountRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var options = new AccountCreateOptions
            {
                // Express: Stripe hosts onboarding, identity verification and
                // the bank-details dashboard; the platform controls payouts.
                Type = "express",
                Email = request.Email,
                Capabilities = new AccountCapabilitiesOptions
                {
                    Transfers = new AccountCapabilitiesTransfersOptions
                    {
                        Requested = true,
                    },
                },
                BusinessProfile = new AccountBusinessProfileOptions
                {
                    Name = request.BusinessName,
                },
                Metadata = new Dictionary<string, string>
                {
                    ["libraryId"] = request.LibraryId.ToString(),
                    ["ownerUserId"] = request.OwnerUserId.ToString(),
                },
            };

            // Country is intentionally omitted: Stripe defaults it to the
            // platform's country, and the owner picks/confirms it during
            // hosted onboarding.
            var requestOptions = new RequestOptions
            {
                IdempotencyKey = request.IdempotencyKey,
            };

            try
            {
                var account = await _accountService.CreateAsync(
                    options,
                    requestOptions,
                    cancellationToken);

                return account.Id;
            }
            catch (StripeException exception)
            {
                throw ToGatewayException(exception, "Stripe could not create the connected account.");
            }
            catch (Exception exception) when (IsTransportFailure(exception, cancellationToken))
            {
                throw ToTransportException(exception);
            }
        }

        public async Task<PayoutOnboardingLink> CreateOnboardingLinkAsync(
            string stripeAccountId,
            Uri returnUrl,
            Uri refreshUrl,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(stripeAccountId))
            {
                throw new ArgumentException(
                    "Stripe account id is required.",
                    nameof(stripeAccountId));
            }

            ArgumentNullException.ThrowIfNull(returnUrl);
            ArgumentNullException.ThrowIfNull(refreshUrl);

            var options = new AccountLinkCreateOptions
            {
                Account = stripeAccountId.Trim(),
                ReturnUrl = returnUrl.AbsoluteUri,
                RefreshUrl = refreshUrl.AbsoluteUri,
                Type = "account_onboarding",
            };

            try
            {
                var link = await _accountLinkService.CreateAsync(
                    options,
                    cancellationToken: cancellationToken);

                return new PayoutOnboardingLink(
                    link.Url,
                    DateTime.SpecifyKind(link.ExpiresAt, DateTimeKind.Utc));
            }
            catch (StripeException exception)
            {
                throw ToGatewayException(exception, "Stripe could not create the onboarding link.");
            }
            catch (Exception exception) when (IsTransportFailure(exception, cancellationToken))
            {
                throw ToTransportException(exception);
            }
        }

        public async Task<string?> CreateExpressDashboardLinkAsync(
            string stripeAccountId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(stripeAccountId))
            {
                throw new ArgumentException(
                    "Stripe account id is required.",
                    nameof(stripeAccountId));
            }

            try
            {
                var loginLink = await _accountLoginLinkService.CreateAsync(
                    stripeAccountId.Trim(),
                    cancellationToken: cancellationToken);

                return loginLink.Url;
            }
            catch (StripeException exception)
                when (exception.HttpStatusCode == HttpStatusCode.BadRequest
                    && string.Equals(
                        exception.StripeError?.Type,
                        "invalid_request_error",
                        StringComparison.Ordinal))
            {
                // Login links exist only for Express accounts; Standard/Custom
                // accounts (attached by id) are rejected with this 400. Any
                // other 4xx (bad key, permissions, rate limit) is an error, not
                // an account-type limitation.
                return null;
            }
            catch (StripeException exception)
            {
                throw ToGatewayException(exception, "Stripe could not create the dashboard link.");
            }
            catch (Exception exception) when (IsTransportFailure(exception, cancellationToken))
            {
                throw ToTransportException(exception);
            }
        }

        /// <summary>
        /// A definitive rejection is a 4xx where Stripe evaluated the REQUEST
        /// and refused it on its merits: nothing was created, and retrying the
        /// same request unchanged cannot succeed, so a fresh idempotency key
        /// may safely re-evaluate and the attempt counts.
        ///
        /// Failures of the CALL rather than the request — bad or rotated API
        /// key, rate limiting, request timeout, Stripe-side faults — are
        /// deliberately excluded: they say nothing about the request and would
        /// otherwise burn a payout's retry budget (and be reported to owners as
        /// a permanent rejection) during an outage.
        /// </summary>
        private static bool IsDefinitiveRejection(StripeException exception)
        {
            if (exception.StripeError is null
                || (int)exception.HttpStatusCode is < 400 or >= 500)
            {
                return false;
            }

            if (exception.HttpStatusCode
                is HttpStatusCode.Unauthorized
                or HttpStatusCode.RequestTimeout
                or HttpStatusCode.TooManyRequests)
            {
                return false;
            }

            return exception.StripeError.Type is not (
                "authentication_error"
                or "rate_limit_error"
                or "api_error");
        }

        private static PayoutGatewayException ToGatewayException(
            StripeException exception,
            string fallbackMessage)
        {
            return new PayoutGatewayException(
                exception.StripeError?.Message ?? fallbackMessage,
                exception,
                IsDefinitiveRejection(exception),
                exception.StripeError?.Code);
        }

        /// <summary>
        /// Stripe.net rethrows connection failures and HTTP timeouts unwrapped;
        /// they must still reach callers as indeterminate gateway failures. The
        /// caller's own cancellation is never swallowed.
        /// </summary>
        private static bool IsTransportFailure(
            Exception exception,
            CancellationToken cancellationToken)
        {
            return exception is HttpRequestException
                || (exception is OperationCanceledException
                    && !cancellationToken.IsCancellationRequested);
        }

        private static PayoutGatewayException ToTransportException(Exception exception)
        {
            return new PayoutGatewayException(
                "Stripe could not be reached.",
                exception);
        }
    }
}
