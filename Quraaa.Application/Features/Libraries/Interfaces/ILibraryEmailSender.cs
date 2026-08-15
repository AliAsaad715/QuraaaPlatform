using Quraaa.Application.Features.Libraries.Common;

namespace Quraaa.Application.Features.Libraries.Interfaces
{
    public interface ILibraryEmailSender
    {
        Task<EmailDeliveryStatus> SendVerificationOtpAsync(
            string recipientEmail,
            string libraryName,
            Guid verificationId,
            string otpCode,
            TimeSpan validity,
            CancellationToken cancellationToken = default);

        Task<EmailDeliveryStatus> SendPasswordResetOtpAsync(
            string recipientEmail,
            string libraryName,
            string otpCode,
            TimeSpan validity,
            CancellationToken cancellationToken = default);

        Task<EmailDeliveryStatus> SendApprovalAsync(
            string recipientEmail,
            string libraryName,
            CancellationToken cancellationToken = default);
    }
}
