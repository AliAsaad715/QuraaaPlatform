namespace Quraaa.Application.Features.Admin.Common
{
    /// <summary>
    /// The phrase a caller must type to delete their own account. Deleting an
    /// account is irreversible, so it takes a deliberate act that cannot be
    /// produced by a mis-click or a replayed request.
    /// </summary>
    public static class AccountDeletionConfirmation
    {
        public const string RequiredPhrase = "DELETE MY ACCOUNT";

        public const string PhraseMismatchMessage =
            "Type \"DELETE MY ACCOUNT\" exactly to confirm.";

        public const string WrongPasswordMessage =
            "The password is incorrect.";

        public static bool Matches(string? confirmationPhrase) =>
            string.Equals(confirmationPhrase?.Trim(), RequiredPhrase, StringComparison.Ordinal);
    }
}
