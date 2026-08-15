using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Quraaa.Application.Features.Libraries.Common;
using Quraaa.Application.Features.Libraries.Interfaces;
using System.Globalization;

namespace Quraaa.Infrastructure.Services
{
    public sealed class SmtpLibraryEmailSender : ILibraryEmailSender
    {
        private readonly SmtpOptions _options;
        private readonly ILogger<SmtpLibraryEmailSender> _logger;

        public SmtpLibraryEmailSender(
            IOptions<SmtpOptions> options,
            ILogger<SmtpLibraryEmailSender> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public async Task<EmailDeliveryStatus> SendVerificationOtpAsync(
            string recipientEmail,
            string libraryName,
            Guid verificationId,
            string otpCode,
            TimeSpan validity,
            CancellationToken cancellationToken = default)
        {
            if (!IsSixAsciiDigits(otpCode)
                || verificationId == Guid.Empty
                || validity <= TimeSpan.Zero
                || string.IsNullOrWhiteSpace(libraryName)
                || !MailboxAddress.TryParse(recipientEmail, out var recipient))
            {
                _logger.LogWarning(
                    "Library verification email was not attempted because its message data was invalid.");
                return EmailDeliveryStatus.NotSent;
            }

            return await SendMessageAsync(
                () => CreateVerificationMessage(
                    recipient,
                    SanitizePlainText(libraryName),
                    verificationId,
                    otpCode,
                    validity),
                LibraryEmailKind.Verification,
                verificationId,
                cancellationToken);
        }

        public async Task<EmailDeliveryStatus> SendApprovalAsync(
            string recipientEmail,
            string libraryName,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(libraryName)
                || !MailboxAddress.TryParse(recipientEmail, out var recipient))
            {
                _logger.LogWarning(
                    "Library approval email was not attempted because its message data was invalid.");
                return EmailDeliveryStatus.NotSent;
            }

            return await SendMessageAsync(
                () => CreateApprovalMessage(
                    recipient,
                    SanitizePlainText(libraryName)),
                LibraryEmailKind.Approval,
                verificationId: null,
                cancellationToken);
        }

        private async Task<EmailDeliveryStatus> SendMessageAsync(
            Func<MimeMessage> messageFactory,
            LibraryEmailKind emailKind,
            Guid? verificationId,
            CancellationToken cancellationToken)
        {
            var sendAttemptStarted = false;
            try
            {
                var message = messageFactory();

                using var smtpClient = new SmtpClient();
                try
                {
                    await smtpClient.ConnectAsync(
                        _options.Host.Trim(),
                        _options.Port,
                        GetSocketOptions(_options.Encryption),
                        cancellationToken);

                    await smtpClient.AuthenticateAsync(
                        _options.Username.Trim(),
                        _options.Password,
                        cancellationToken);

                    // Once SendAsync starts, a transport interruption or caller
                    // cancellation cannot prove whether the SMTP server accepted the
                    // message. Only an explicit SMTP command rejection is definite.
                    sendAttemptStarted = true;
                    await smtpClient.SendAsync(message, cancellationToken);
                    return EmailDeliveryStatus.Sent;
                }
                finally
                {
                    if (smtpClient.IsConnected)
                    {
                        try
                        {
                            await smtpClient.DisconnectAsync(
                                quit: true,
                                CancellationToken.None);
                        }
                        catch
                        {
                            // Delivery has already succeeded or failed. A disconnect
                            // failure must not expose provider details or trigger an
                            // unnecessary second delivery attempt.
                        }
                    }
                }
            }
            catch (SmtpCommandException exception) when (sendAttemptStarted)
            {
                LogExplicitRejection(exception, emailKind, verificationId);
                return EmailDeliveryStatus.NotSent;
            }
            catch (Exception exception) when (!sendAttemptStarted)
            {
                LogPreSendFailure(exception, emailKind, verificationId);
                return EmailDeliveryStatus.NotSent;
            }
            catch (Exception exception)
            {
                LogUnknownOutcome(exception, emailKind, verificationId);
                return EmailDeliveryStatus.Unknown;
            }
        }

        private MimeMessage CreateVerificationMessage(
            MailboxAddress recipient,
            string libraryName,
            Guid verificationId,
            string otpCode,
            TimeSpan validity)
        {
            var validityMinutes = Math.Max(1, (long)Math.Ceiling(validity.TotalMinutes));
            var validityDescription = validityMinutes == 1
                ? "1 minute"
                : $"{validityMinutes.ToString(CultureInfo.InvariantCulture)} minutes";

            return CreateMessage(
                recipient,
                "Verify your library email address",
                $"Use this one-time code to verify the email address for {libraryName}:\n\n" +
                $"{otpCode}\n\n" +
                $"Verification ID: {verificationId:D}\n\n" +
                $"This code is valid for {validityDescription}.\n\n" +
                "If you did not submit this library registration, you can ignore this email.");
        }

        private MimeMessage CreateApprovalMessage(
            MailboxAddress recipient,
            string libraryName) =>
            CreateMessage(
                recipient,
                "Your library registration has been approved",
                $"Your registration for {libraryName} has been approved.\n\n" +
                "You can now sign in and manage your library on Quraaa Platform.\n\n" +
                "Thank you for joining Quraaa Platform.");

        private MimeMessage CreateMessage(
            MailboxAddress recipient,
            string subject,
            string body)
        {
            if (!MailboxAddress.TryParse(_options.FromAddress.Trim(), out var fromAddress))
            {
                throw new InvalidOperationException("The SMTP sender address is invalid.");
            }

            var message = new MimeMessage
            {
                Date = DateTimeOffset.UtcNow,
                Subject = subject,
                Body = new TextPart("plain") { Text = body }
            };

            message.From.Add(new MailboxAddress(
                SanitizePlainText(_options.FromName),
                fromAddress.Address));
            message.To.Add(recipient);

            return message;
        }

        private void LogExplicitRejection(
            Exception exception,
            LibraryEmailKind emailKind,
            Guid? verificationId)
        {
            if (emailKind == LibraryEmailKind.Verification)
            {
                _logger.LogWarning(
                    exception,
                    "SMTP explicitly rejected library verification email {VerificationId}.",
                    verificationId);
                return;
            }

            _logger.LogWarning(exception, "SMTP explicitly rejected library approval email.");
        }

        private void LogPreSendFailure(
            Exception exception,
            LibraryEmailKind emailKind,
            Guid? verificationId)
        {
            if (emailKind == LibraryEmailKind.Verification)
            {
                _logger.LogWarning(
                    exception,
                    "Library verification email {VerificationId} failed before SMTP send started.",
                    verificationId);
                return;
            }

            _logger.LogWarning(
                exception,
                "Library approval email failed before SMTP send started.");
        }

        private void LogUnknownOutcome(
            Exception exception,
            LibraryEmailKind emailKind,
            Guid? verificationId)
        {
            if (emailKind == LibraryEmailKind.Verification)
            {
                _logger.LogWarning(
                    exception,
                    "Library verification email {VerificationId} has an unknown SMTP delivery outcome.",
                    verificationId);
                return;
            }

            _logger.LogWarning(
                exception,
                "Library approval email has an unknown SMTP delivery outcome.");
        }

        private static SecureSocketOptions GetSocketOptions(string encryption) =>
            encryption.Trim().ToLowerInvariant() switch
            {
                "tls" or "starttls" => SecureSocketOptions.StartTls,
                "ssl" or "smtps" => SecureSocketOptions.SslOnConnect,
                _ => throw new InvalidOperationException("The SMTP encryption mode is invalid.")
            };

        private static string SanitizePlainText(string value) =>
            value
                .Replace('\r', ' ')
                .Replace('\n', ' ')
                .Trim();

        private static bool IsSixAsciiDigits(string code) =>
            code is { Length: 6 }
            && code.All(character => character is >= '0' and <= '9');

        private enum LibraryEmailKind
        {
            Verification,
            Approval
        }
    }
}
