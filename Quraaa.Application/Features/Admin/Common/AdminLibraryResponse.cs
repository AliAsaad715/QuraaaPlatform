using Quraaa.Domain.Library.Enums;

namespace Quraaa.Application.Features.Admin.Common
{
    /// <summary>A library as administrators see it.</summary>
    public record AdminLibraryResponse(
        Guid LibraryId,
        string LibraryName,
        string Email,
        string Location,
        LibraryApprovalStatus ApprovalStatus,
        Guid OwnerUserId,
        string OwnerName,
        int ListingCount,
        bool IsDeactivated,
        DateTime? DeactivatedAtUtc,
        DateTime CreatedAt);
}
