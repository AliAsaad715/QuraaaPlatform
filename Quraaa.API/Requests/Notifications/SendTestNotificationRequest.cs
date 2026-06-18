namespace Quraaa.API.Requests.Notifications
{
    public record SendTestNotificationRequest(
        string DeviceToken,
        string? Title = null,
        string? Body = null,
        Dictionary<string, string>? Data = null
    );
}
