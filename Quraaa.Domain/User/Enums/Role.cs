namespace Quraaa.Domain.User.Enums
{
    public enum Role
    {
        User = 1,
        Admin = 2,
        LibraryOwner = 3,

        /// <summary>
        /// Full platform authority: everything an <see cref="Admin"/> can do,
        /// plus creating other administrators. Super admins hold BOTH the Admin
        /// and SuperAdmin identity roles, so ordinary admin endpoints keep
        /// working for them unchanged.
        /// </summary>
        SuperAdmin = 4,
    }
}
