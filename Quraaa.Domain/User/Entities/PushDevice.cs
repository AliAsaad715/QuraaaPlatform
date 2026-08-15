using Quraaa.Domain.Shared.Entities;
using Quraaa.Domain.Shared.Exceptions;
using System.Security.Cryptography;
using System.Text;

namespace Quraaa.Domain.User.Entities;

public sealed class PushDevice : AuditableEntity
{
    public const int TokenMaxLength = 4096;
    public const int TokenHashLength = 64;

    public Guid UserId { get; private set; }
    public string Token { get; private set; } = null!;
    public string TokenHash { get; private set; } = null!;

    private PushDevice() { }

    private PushDevice(Guid id, Guid userId, string token)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("User id is required.");
        }

        var validatedToken = ValidateToken(token);

        Id = id;
        UserId = userId;
        Token = validatedToken;
        TokenHash = ComputeValidatedTokenHash(validatedToken);
    }

    public static PushDevice Create(Guid userId, string token) =>
        new(Guid.NewGuid(), userId, token);

    public static string ComputeTokenHash(string token)
    {
        var validatedToken = ValidateToken(token);
        return ComputeValidatedTokenHash(validatedToken);
    }

    private static string ValidateToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new DomainException("Device token is required.");
        }

        if (token.Length > TokenMaxLength)
        {
            throw new DomainException($"Device token cannot exceed {TokenMaxLength} characters.");
        }

        if (token.Any(character => char.IsWhiteSpace(character) || char.IsControl(character)))
        {
            throw new DomainException("Device token cannot contain whitespace or control characters.");
        }

        return token;
    }

    private static string ComputeValidatedTokenHash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

}
