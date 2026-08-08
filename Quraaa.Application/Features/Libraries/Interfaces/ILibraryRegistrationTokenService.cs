namespace Quraaa.Application.Features.Libraries.Interfaces
{
    public sealed record LibraryRegistrationToken(
        string RawToken,
        string TokenHash);

    public interface ILibraryRegistrationTokenService
    {
        LibraryRegistrationToken Generate();

        bool TryHash(string rawToken, out string tokenHash);
    }
}
