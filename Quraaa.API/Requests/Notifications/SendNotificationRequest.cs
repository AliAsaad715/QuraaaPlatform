namespace Quraaa.API.Requests.Notifications
{
    public record SendNotificationRequest(
        string DeviceToken,
        string Title,
        string Body,
        Dictionary<string, string>? Data = null
    );
}
