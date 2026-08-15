using Quraaa.Domain.Library.Enums;

namespace Quraaa.Application.Features.Libraries.Common
{
    public enum LibraryRegistrationStage
    {
        DetailsRequired = 1,
        EmailVerificationRequired = 2,

        /// <summary>
        /// Email verified; the owner may now connect a Stripe wallet through
        /// Stripe-hosted onboarding (optional — it can also be done later from
        /// the owner dashboard once the library is approved).
        /// </summary>
        StripeWalletSetup = 3,

        /// <summary>The wizard is finished; the application awaits admin review.</summary>
        Completed = 4
    }

    public enum EmailDeliveryStatus
    {
        Sent = 1,
        NotSent = 2,
        Unknown = 3
    }

    public sealed record LibraryRegistrationLinkResponse(
        string RegistrationUrl,
        DateTime ExpiresAtUtc);

    public sealed record LibraryRegistrationContextResponse(
        LibraryRegistrationStage Stage,
        DateTime SessionExpiresAtUtc,
        Guid? LibraryId,
        Guid? VerificationId,
        string? MaskedEmail,
        DateTime? OtpExpiresAtUtc,
        DateTime? ResendAvailableAtUtc,
        LibraryWalletStatus? WalletStatus = null,
        string? StripeAccountId = null);

    public sealed record LibraryRegistrationSubmissionResponse(
        Guid LibraryId,
        Guid VerificationId,
        LibraryApprovalStatus ApprovalStatus,
        string MaskedEmail,
        EmailDeliveryStatus EmailDeliveryStatus,
        DateTime OtpExpiresAtUtc,
        DateTime ResendAvailableAtUtc,
        DateTime SessionExpiresAtUtc);

    public sealed record LibraryEmailOtpResponse(
        Guid LibraryId,
        Guid VerificationId,
        string MaskedEmail,
        EmailDeliveryStatus EmailDeliveryStatus,
        DateTime OtpExpiresAtUtc,
        DateTime ResendAvailableAtUtc);

    /// <param name="RegistrationToken">
    /// A freshly issued registration token that replaces the one used to
    /// verify: the wizard must use it for the remaining (Stripe wallet) step.
    /// Null once the wizard is finished. Rotating here means a copy of the
    /// original link cannot later bind the library's payout account.
    /// </param>
    public sealed record LibraryEmailVerificationResponse(
        Guid LibraryId,
        Guid VerificationId,
        LibraryApprovalStatus ApprovalStatus,
        DateTime EmailVerifiedAtUtc,
        LibraryRegistrationStage NextStage,
        DateTime SessionExpiresAtUtc,
        string? RegistrationToken);

    public static class LibraryEmailMasker
    {
        public static string Mask(string email)
        {
            var separatorIndex = email.IndexOf('@');
            if (separatorIndex <= 0 || separatorIndex == email.Length - 1)
            {
                return "***";
            }

            var localPart = email[..separatorIndex];
            var domain = email[(separatorIndex + 1)..];
            var visiblePrefix = localPart[..Math.Min(2, localPart.Length)];
            return $"{visiblePrefix}***@{domain}";
        }
    }
}
