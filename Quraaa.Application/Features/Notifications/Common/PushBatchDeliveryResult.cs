namespace Quraaa.Application.Features.Notifications.Common;

public sealed record PushBatchDeliveryResult(
    IReadOnlyCollection<string> SuccessfulDeviceTokens,
    IReadOnlyCollection<string> InvalidDeviceTokens,
    IReadOnlyCollection<string> TransientFailureDeviceTokens)
{
    public int SuccessCount => SuccessfulDeviceTokens.Count;
    public bool HasTransientFailures => TransientFailureDeviceTokens.Count > 0;
}
