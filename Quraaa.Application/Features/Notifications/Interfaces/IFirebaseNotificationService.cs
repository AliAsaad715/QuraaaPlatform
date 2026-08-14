using Quraaa.Application.Features.Notifications.Common;

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

        /// <summary>
        /// Best-effort fan-out to many devices at once (chunked to FCM's 500-token-per-request
        /// limit). Never throws — per-token and per-chunk failures are reported back in the
        /// result instead, since callers treat delivery as best-effort.
        /// </summary>
        Task<FirebaseMulticastResult> SendMulticastAsync(
            IReadOnlyCollection<string> deviceTokens,
            string title,
            string body,
            IReadOnlyDictionary<string, string>? data = null,
            CancellationToken cancellationToken = default);
    }
}
