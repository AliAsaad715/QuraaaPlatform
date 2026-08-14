namespace Quraaa.Persistence.Data
{
    public sealed class UserDeviceToken
    {
        private UserDeviceToken()
        {
        }

        public UserDeviceToken(Guid userId, string deviceToken, DateTime registeredAtUtc)
        {
            Id = Guid.NewGuid();
            UserId = userId;
            DeviceToken = deviceToken;
            RegisteredAtUtc = registeredAtUtc;
            LastSeenAtUtc = registeredAtUtc;
        }

        public Guid Id { get; private set; }
        public Guid UserId { get; private set; }
        public string DeviceToken { get; private set; } = null!;
        public DateTime RegisteredAtUtc { get; private set; }
        public DateTime LastSeenAtUtc { get; private set; }

        public void Reassign(Guid userId, DateTime nowUtc)
        {
            UserId = userId;
            LastSeenAtUtc = nowUtc;
        }

        public void Touch(DateTime nowUtc)
        {
            LastSeenAtUtc = nowUtc;
        }
    }
}
