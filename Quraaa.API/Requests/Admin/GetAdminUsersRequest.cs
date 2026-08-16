using Quraaa.Domain.User.Enums;

namespace Quraaa.API.Requests.Admin
{
    public class GetAdminUsersRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SearchTerm { get; set; }

        /// <summary>Include deactivated (soft-deleted) accounts.</summary>
        public bool IncludeDeactivated { get; set; }

        public Role? Role { get; set; }
    }

    public class GetAdminRecordsRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SearchTerm { get; set; }
        public bool IncludeDeactivated { get; set; }
    }
}
