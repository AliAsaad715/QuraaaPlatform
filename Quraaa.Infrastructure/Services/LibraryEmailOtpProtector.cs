using Microsoft.Extensions.Options;
using Quraaa.Application.Features.Libraries.Interfaces;
using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Quraaa.Infrastructure.Services
{
    public sealed class LibraryEmailOtpProtector : ILibraryEmailOtpProtector, IDisposable
    {
        private const string CodeDerivationPurpose =
            "quraaa:library-email-verification-code-derivation:v1";
        private const string CodeHashPurpose =
            "quraaa:library-email-verification-code-hash:v1";
        private const string HashPrefix = "hmac-sha256:";
        private const int OtpLength = 6;
        private const ulong OtpRange = 1_000_000;

        private readonly byte[] _pepperBytes;
        private bool _disposed;

        public LibraryEmailOtpProtector(IOptions<LibraryEmailOtpOptions> options)
        {
            _pepperBytes = Encoding.UTF8.GetBytes(options.Value.Pepper);
        }

        public string DeriveCode(
            Guid libraryId,
            Guid userId,
            string normalizedEmail,
            Guid generation)
        {
            ThrowIfDisposed();
            ValidateDerivationContext(libraryId, userId, normalizedEmail, generation);

            var contextBytes = BuildContext(
                CodeDerivationPurpose,
                libraryId,
                userId,
                normalizedEmail,
                generation,
                otpCode: null);

            try
            {
                var digest = ComputeHmac(contextBytes);
                try
                {
                    var numericCode = BinaryPrimitives.ReadUInt64BigEndian(digest) % OtpRange;
                    return numericCode.ToString("D6", CultureInfo.InvariantCulture);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(digest);
                }
            }
            finally
            {
                CryptographicOperations.ZeroMemory(contextBytes);
            }
        }

        public string HashCode(
            string otpCode,
            Guid libraryId,
            Guid userId,
            string normalizedEmail,
            Guid generation)
        {
            ThrowIfDisposed();
            ValidateContext(otpCode, libraryId, userId, normalizedEmail, generation);

            var hash = ComputeCodeHash(
                otpCode,
                libraryId,
                userId,
                normalizedEmail,
                generation);

            try
            {
                return $"{HashPrefix}{Convert.ToBase64String(hash)}";
            }
            finally
            {
                CryptographicOperations.ZeroMemory(hash);
            }
        }

        public bool VerifyCode(
            string otpCode,
            Guid libraryId,
            Guid userId,
            string normalizedEmail,
            Guid generation,
            string expectedHash)
        {
            ThrowIfDisposed();

            if (!IsValidContext(otpCode, libraryId, userId, normalizedEmail, generation)
                || string.IsNullOrEmpty(expectedHash)
                || !expectedHash.StartsWith(HashPrefix, StringComparison.Ordinal))
            {
                return false;
            }

            Span<byte> expectedHashBytes = stackalloc byte[HMACSHA256.HashSizeInBytes];
            if (!Convert.TryFromBase64String(
                    expectedHash[HashPrefix.Length..],
                    expectedHashBytes,
                    out var bytesWritten)
                || bytesWritten != HMACSHA256.HashSizeInBytes)
            {
                CryptographicOperations.ZeroMemory(expectedHashBytes);
                return false;
            }

            var actualHashBytes = ComputeCodeHash(
                otpCode,
                libraryId,
                userId,
                normalizedEmail,
                generation);

            try
            {
                return CryptographicOperations.FixedTimeEquals(
                    actualHashBytes,
                    expectedHashBytes);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(actualHashBytes);
                CryptographicOperations.ZeroMemory(expectedHashBytes);
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            CryptographicOperations.ZeroMemory(_pepperBytes);
            _disposed = true;
        }

        private byte[] ComputeCodeHash(
            string otpCode,
            Guid libraryId,
            Guid userId,
            string normalizedEmail,
            Guid generation)
        {
            var contextBytes = BuildContext(
                CodeHashPurpose,
                libraryId,
                userId,
                normalizedEmail,
                generation,
                otpCode);

            try
            {
                return ComputeHmac(contextBytes);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(contextBytes);
            }
        }

        private static byte[] BuildContext(
            string purpose,
            Guid libraryId,
            Guid userId,
            string normalizedEmail,
            Guid generation,
            string? otpCode)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);

            WriteLengthPrefixed(writer, purpose);
            writer.Write(libraryId.ToByteArray());
            writer.Write(userId.ToByteArray());
            WriteLengthPrefixed(writer, normalizedEmail);
            writer.Write(generation.ToByteArray());
            if (otpCode is not null)
            {
                WriteLengthPrefixed(writer, otpCode);
            }
            writer.Flush();

            return stream.ToArray();
        }

        private byte[] ComputeHmac(byte[] contextBytes)
        {
            using var hmac = new HMACSHA256(_pepperBytes);
            return hmac.ComputeHash(contextBytes);
        }

        private static void WriteLengthPrefixed(BinaryWriter writer, string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            try
            {
                writer.Write(bytes.Length);
                writer.Write(bytes);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(bytes);
            }
        }

        private static void ValidateContext(
            string otpCode,
            Guid libraryId,
            Guid userId,
            string normalizedEmail,
            Guid generation)
        {
            if (!IsSixAsciiDigits(otpCode))
            {
                throw new ArgumentException(
                    "The OTP code must contain exactly six ASCII digits.",
                    nameof(otpCode));
            }

            ValidateDerivationContext(libraryId, userId, normalizedEmail, generation);
        }

        private static void ValidateDerivationContext(
            Guid libraryId,
            Guid userId,
            string normalizedEmail,
            Guid generation)
        {
            if (libraryId == Guid.Empty)
            {
                throw new ArgumentException("The library identifier is required.", nameof(libraryId));
            }

            if (userId == Guid.Empty)
            {
                throw new ArgumentException("The user identifier is required.", nameof(userId));
            }

            if (!IsNormalizedEmail(normalizedEmail))
            {
                throw new ArgumentException(
                    "A normalized email address is required.",
                    nameof(normalizedEmail));
            }

            if (generation == Guid.Empty)
            {
                throw new ArgumentException(
                    "The OTP generation identifier is required.",
                    nameof(generation));
            }
        }

        private static bool IsValidContext(
            string otpCode,
            Guid libraryId,
            Guid userId,
            string normalizedEmail,
            Guid generation) =>
            IsSixAsciiDigits(otpCode)
            && libraryId != Guid.Empty
            && userId != Guid.Empty
            && IsNormalizedEmail(normalizedEmail)
            && generation != Guid.Empty;

        private static bool IsSixAsciiDigits(string otpCode) =>
            otpCode is { Length: OtpLength }
            && otpCode.All(character => character is >= '0' and <= '9');

        private static bool IsNormalizedEmail(string normalizedEmail) =>
            !string.IsNullOrWhiteSpace(normalizedEmail)
            && string.Equals(
                normalizedEmail,
                normalizedEmail.Trim().ToLowerInvariant(),
                StringComparison.Ordinal);

        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
        }
    }
}
