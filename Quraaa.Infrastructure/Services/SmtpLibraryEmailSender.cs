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

            var sendAttemptStarted = false;
            try
            {
                var message = CreateMessage(
                    recipient,
                    SanitizePlainText(libraryName),
                    verificationId,
                    otpCode,
                    validity);

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
                _logger.LogWarning(
                    exception,
                    "SMTP explicitly rejected library verification email {VerificationId}.",
                    verificationId);
                return EmailDeliveryStatus.NotSent;
            }
            catch (Exception exception) when (!sendAttemptStarted)
            {
                _logger.LogWarning(
                    exception,
                    "Library verification email {VerificationId} failed before SMTP send started.",
                    verificationId);
                return EmailDeliveryStatus.NotSent;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Library verification email {VerificationId} has an unknown SMTP delivery outcome.",
                    verificationId);
                return EmailDeliveryStatus.Unknown;
            }
        }

        private MimeMessage CreateMessage(
            MailboxAddress recipient,
            string libraryName,
            Guid verificationId,
            string otpCode,
            TimeSpan validity)
        {
            if (!MailboxAddress.TryParse(_options.FromAddress.Trim(), out var fromAddress))
            {
                throw new InvalidOperationException("The SMTP sender address is invalid.");
            }

            var validityMinutes = Math.Max(1, (long)Math.Ceiling(validity.TotalMinutes));
            var validityDescription = validityMinutes == 1
                ? "1 minute"
                : $"{validityMinutes.ToString(CultureInfo.InvariantCulture)} minutes";

            var message = new MimeMessage
            {
                Date = DateTimeOffset.UtcNow,
                Subject = "Verify your library email address",
                Body = new TextPart("plain")
                {
                    Text =
                        $"Use this one-time code to verify the email address for {libraryName}:\n\n" +
                        $"{otpCode}\n\n" +
                        $"Verification ID: {verificationId:D}\n\n" +
                        $"This code is valid for {validityDescription}.\n\n" +
                        "If you did not submit this library registration, you can ignore this email."
                }
            };

            message.From.Add(new MailboxAddress(
                SanitizePlainText(_options.FromName),
                fromAddress.Address));
            message.To.Add(recipient);

            return message;
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
    }
}
