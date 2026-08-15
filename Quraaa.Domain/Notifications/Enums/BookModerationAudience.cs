namespace Quraaa.Domain.Notifications.Enums
{
    /// <summary>Who a book moderation notice is addressed to.</summary>
    public enum BookModerationAudience
    {
        /// <summary>A library that currently lists the reported book.</summary>
        Library = 1,

        /// <summary>A platform administrator.</summary>
        Admin = 2,
    }
}
