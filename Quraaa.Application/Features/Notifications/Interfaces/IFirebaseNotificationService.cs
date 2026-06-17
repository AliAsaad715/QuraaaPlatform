namespace Quraaa.Application.Features.Notifications.Interfaces
{
    public interface IFirebaseNotificationService
    {
        Task<string> SendToDeviceAsync(
            string deviceToken,
            string title,
            string body,
            IReadOnlyDictionary<string, string>? data = null,
            CancellationToken cancellationToken = default);
    }
}
