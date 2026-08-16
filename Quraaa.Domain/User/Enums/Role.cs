namespace Quraaa.Domain.User.Enums
{
    public enum Role
    {
        User = 1,

        // Value 2 belonged to the retired Admin role. Do not reuse it: roles
        // are persisted as integers and legacy Admin rows are migrated to 4.
        LibraryOwner = 3,

        /// <summary>
        /// Full platform authority for every administrative operation.
        /// </summary>
        SuperAdmin = 4,
    }
}
