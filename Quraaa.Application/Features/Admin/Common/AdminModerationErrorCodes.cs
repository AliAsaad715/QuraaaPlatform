namespace Quraaa.Application.Features.Admin.Common
{
    public static class AdminModerationErrorCodes
    {
        public const string NotFound = "Not found.";

        /// <summary>
        /// Permanent removal is only ever allowed from the deactivated state, so
        /// a record is always parked (and recoverable) before it can be lost.
        /// </summary>
        public const string MustBeDeactivatedFirst =
            "Deactivate this record before deleting it permanently.";

        public const string StillReferenced =
            "This record cannot be deleted while other records still reference it.";

        public const string CannotTargetSelf =
            "You cannot apply this action to your own account.";

        public const string LastSuperAdmin =
            "The last super admin cannot be removed; promote another one first.";
    }
}
