using Quraaa.Domain.Orders.Enums;
using Quraaa.Domain.Shared.Entities;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Domain.Orders.Entities
{
    public class PaymentAttempt : Entity
    {
        public Guid OrderId { get; private set; }
        public int AttemptNumber { get; private set; }
        public PaymentProvider Provider { get; private set; }
        public PaymentAttemptStatus Status { get; private set; }
        public long AmountMinor { get; private set; }
        public string Currency { get; private set; } = null!;
        public string SuccessUrl { get; private set; } = null!;
        public string CancelUrl { get; private set; } = null!;
        public string? CheckoutSessionId { get; private set; }
        public string? CheckoutUrl { get; private set; }
        public string? PaymentIntentId { get; private set; }
        public DateTime? ExpiresAtUtc { get; private set; }
        public DateTime? CompletedAtUtc { get; private set; }
        public string? FailureCode { get; private set; }
        public string? FailureMessage { get; private set; }

        private PaymentAttempt() { }

        private PaymentAttempt(
            Guid id,
            Guid orderId,
            int attemptNumber,
            PaymentProvider provider,
            long amountMinor,
            string currency,
            string successUrl,
            string cancelUrl,
            DateTime expiresAtUtc)
        {
            Id = id;
            OrderId = orderId;
            AttemptNumber = attemptNumber;
            Provider = provider;
            Status = PaymentAttemptStatus.Created;
            AmountMinor = amountMinor;
            Currency = currency;
            SuccessUrl = successUrl.Trim();
            CancelUrl = cancelUrl.Trim();
            ExpiresAtUtc = expiresAtUtc;
        }

        internal static PaymentAttempt Create(
            Guid orderId,
            int attemptNumber,
            PaymentProvider provider,
            long amountMinor,
            string currency,
            string successUrl,
            string cancelUrl,
            DateTime expiresAtUtc)
        {
            if (orderId == Guid.Empty)
            {
                throw new DomainException("Order id is required for a payment attempt.");
            }

            if (attemptNumber <= 0)
            {
                throw new DomainException("Payment attempt number must be greater than zero.");
            }

            if (!Enum.IsDefined(provider))
            {
                throw new DomainException("Payment provider is invalid.");
            }

            if (amountMinor <= 0)
            {
                throw new DomainException("Payment amount must be greater than zero.");
            }

            if (string.IsNullOrWhiteSpace(currency))
            {
                throw new DomainException("Payment currency is required.");
            }

            if (string.IsNullOrWhiteSpace(successUrl))
            {
                throw new DomainException("Payment success URL is required.");
            }

            if (successUrl.Trim().Length > 2048)
            {
                throw new DomainException("Payment success URL cannot exceed 2048 characters.");
            }

            if (string.IsNullOrWhiteSpace(cancelUrl))
            {
                throw new DomainException("Payment cancel URL is required.");
            }

            if (cancelUrl.Trim().Length > 2048)
            {
                throw new DomainException("Payment cancel URL cannot exceed 2048 characters.");
            }

            if (expiresAtUtc <= DateTime.UtcNow)
            {
                throw new DomainException("Payment attempt expiry must be in the future.");
            }

            return new PaymentAttempt(
                Guid.NewGuid(),
                orderId,
                attemptNumber,
                provider,
                amountMinor,
                currency,
                successUrl,
                cancelUrl,
                expiresAtUtc);
        }

        internal void AttachCheckoutSession(
            string checkoutSessionId,
            string checkoutUrl,
            DateTime? expiresAtUtc)
        {
            if (string.IsNullOrWhiteSpace(checkoutSessionId))
            {
                throw new DomainException("Checkout session id is required.");
            }

            if (checkoutSessionId.Trim().Length > 255)
            {
                throw new DomainException("Checkout session id cannot exceed 255 characters.");
            }

            if (string.IsNullOrWhiteSpace(checkoutUrl))
            {
                throw new DomainException("Checkout URL is required.");
            }

            if (checkoutUrl.Trim().Length > 2048)
            {
                throw new DomainException("Checkout URL cannot exceed 2048 characters.");
            }

            if (Status == PaymentAttemptStatus.CheckoutCreated)
            {
                if (string.Equals(
                        CheckoutSessionId,
                        checkoutSessionId.Trim(),
                        StringComparison.Ordinal))
                {
                    return;
                }

                throw new ConflictException("A different checkout session is already attached.");
            }

            if (Status != PaymentAttemptStatus.Created)
            {
                throw new ConflictException("A checkout session cannot be attached to this payment attempt.");
            }

            DateTime? normalizedExpiry = expiresAtUtc is null
                ? null
                : NormalizeUtc(expiresAtUtc.Value);

            if (normalizedExpiry is not null && normalizedExpiry <= DateTime.UtcNow)
            {
                throw new DomainException("Checkout session expiry must be in the future.");
            }

            CheckoutSessionId = checkoutSessionId.Trim();
            CheckoutUrl = checkoutUrl.Trim();

            if (normalizedExpiry is not null)
            {
                ExpiresAtUtc = normalizedExpiry.Value;
            }

            Status = PaymentAttemptStatus.CheckoutCreated;
        }

        internal void MarkSucceeded(string? paymentIntentId)
        {
            var normalizedPaymentIntentId = NormalizeOptional(paymentIntentId);

            if (normalizedPaymentIntentId is not null
                && normalizedPaymentIntentId.Length > 255)
            {
                throw new DomainException("Payment intent id cannot exceed 255 characters.");
            }

            if (Status == PaymentAttemptStatus.Succeeded)
            {
                if (PaymentIntentId is null && normalizedPaymentIntentId is not null)
                {
                    PaymentIntentId = normalizedPaymentIntentId;
                    return;
                }

                if (normalizedPaymentIntentId is null ||
                    string.Equals(
                        PaymentIntentId,
                        normalizedPaymentIntentId,
                        StringComparison.Ordinal))
                {
                    return;
                }

                throw new ConflictException("A different payment intent is already attached.");
            }

            EnsureActive();

            PaymentIntentId = normalizedPaymentIntentId;
            Status = PaymentAttemptStatus.Succeeded;
            CompletedAtUtc = DateTime.UtcNow;
            FailureCode = null;
            FailureMessage = null;
        }

        internal void MarkFailed(string? failureCode, string? failureMessage)
        {
            if (Status == PaymentAttemptStatus.Failed)
            {
                return;
            }

            EnsureActive();

            var normalizedFailureCode = NormalizeOptional(failureCode);
            var normalizedFailureMessage = NormalizeOptional(failureMessage);

            if (normalizedFailureCode is { Length: > 100 })
            {
                throw new DomainException("Payment failure code cannot exceed 100 characters.");
            }

            if (normalizedFailureMessage is { Length: > 1000 })
            {
                throw new DomainException("Payment failure message cannot exceed 1000 characters.");
            }

            Status = PaymentAttemptStatus.Failed;
            CompletedAtUtc = DateTime.UtcNow;
            FailureCode = normalizedFailureCode;
            FailureMessage = normalizedFailureMessage;
        }

        internal void MarkCancelled()
        {
            if (Status == PaymentAttemptStatus.Cancelled)
            {
                return;
            }

            EnsureActive();

            Status = PaymentAttemptStatus.Cancelled;
            CompletedAtUtc = DateTime.UtcNow;
        }

        internal void MarkExpired()
        {
            if (Status == PaymentAttemptStatus.Expired)
            {
                return;
            }

            EnsureActive();

            Status = PaymentAttemptStatus.Expired;
            CompletedAtUtc = DateTime.UtcNow;
        }

        private void EnsureActive()
        {
            if (Status is not (
                PaymentAttemptStatus.Created or
                PaymentAttemptStatus.CheckoutCreated))
            {
                throw new ConflictException("The payment attempt is already terminal.");
            }
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static DateTime NormalizeUtc(DateTime value)
        {
            return value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
            };
        }
    }
}
