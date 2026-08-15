namespace Quraaa.Application.Features.Notifications.Common
{
    public sealed record FirebaseMulticastResult(
        IReadOnlyCollection<string> SuccessfulTokens,
        IReadOnlyCollection<string> InvalidTokens,
        IReadOnlyCollection<string> TransientFailureTokens)
    {
        public int SuccessCount => SuccessfulTokens.Count;
        public int FailureCount => InvalidTokens.Count + TransientFailureTokens.Count;
        public bool HasTransientFailures => TransientFailureTokens.Count > 0;
    }
}
