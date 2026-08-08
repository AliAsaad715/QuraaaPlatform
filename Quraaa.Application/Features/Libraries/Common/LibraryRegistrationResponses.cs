using Quraaa.Domain.Library.Enums;

namespace Quraaa.Application.Features.Libraries.Common
{
    public enum LibraryRegistrationStage
    {
        DetailsRequired = 1,
        EmailVerificationRequired = 2
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
        DateTime? ResendAvailableAtUtc);

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

    public sealed record LibraryEmailVerificationResponse(
        Guid LibraryId,
        Guid VerificationId,
        LibraryApprovalStatus ApprovalStatus,
        DateTime EmailVerifiedAtUtc);

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
