using Quraaa.Application.Features.Libraries.Interfaces;
using System.Security.Cryptography;

namespace Quraaa.Infrastructure.Services
{
    public sealed class LibraryRegistrationTokenService : ILibraryRegistrationTokenService
    {
        private const int TokenByteCount = 32;
        private const int EncodedTokenLength = 43;
        private const string HashPrefix = "sha256:";

        public LibraryRegistrationToken Generate()
        {
            var tokenBytes = RandomNumberGenerator.GetBytes(TokenByteCount);
            var rawToken = EncodeBase64Url(tokenBytes);
            var tokenHash = FormatHash(SHA256.HashData(tokenBytes));

            CryptographicOperations.ZeroMemory(tokenBytes);
            return new LibraryRegistrationToken(rawToken, tokenHash);
        }

        public bool TryHash(string rawToken, out string tokenHash)
        {
            tokenHash = string.Empty;

            if (string.IsNullOrEmpty(rawToken)
                || rawToken.Length != EncodedTokenLength
                || rawToken.StartsWith(HashPrefix, StringComparison.OrdinalIgnoreCase)
                || rawToken.Any(character => !IsBase64UrlCharacter(character)))
            {
                return false;
            }

            Span<char> paddedToken = stackalloc char[44];
            rawToken.AsSpan().CopyTo(paddedToken);
            paddedToken[43] = '=';

            for (var index = 0; index < rawToken.Length; index++)
            {
                paddedToken[index] = paddedToken[index] switch
                {
                    '-' => '+',
                    '_' => '/',
                    var character => character
                };
            }

            Span<byte> tokenBytes = stackalloc byte[TokenByteCount];
            if (!Convert.TryFromBase64Chars(
                    paddedToken,
                    tokenBytes,
                    out var bytesWritten)
                || bytesWritten != TokenByteCount
                || !string.Equals(
                    EncodeBase64Url(tokenBytes),
                    rawToken,
                    StringComparison.Ordinal))
            {
                CryptographicOperations.ZeroMemory(tokenBytes);
                return false;
            }

            Span<byte> hashBytes = stackalloc byte[SHA256.HashSizeInBytes];
            SHA256.HashData(tokenBytes, hashBytes);
            tokenHash = FormatHash(hashBytes);

            CryptographicOperations.ZeroMemory(tokenBytes);
            CryptographicOperations.ZeroMemory(hashBytes);
            return true;
        }

        private static bool IsBase64UrlCharacter(char character) =>
            character is >= 'A' and <= 'Z'
                or >= 'a' and <= 'z'
                or >= '0' and <= '9'
                or '-'
                or '_';

        private static string EncodeBase64Url(ReadOnlySpan<byte> value) =>
            Convert.ToBase64String(value)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');

        private static string FormatHash(ReadOnlySpan<byte> hash) =>
            $"{HashPrefix}{Convert.ToBase64String(hash)}";
    }
}
