namespace Quraaa.Application.Features.Notifications.Common
{
    public sealed record FirebaseMulticastResult(
        int SuccessCount,
        int FailureCount,
        IReadOnlyCollection<string> InvalidTokens);
}
