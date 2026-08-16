using Quraaa.Domain.User.Enums;

namespace Quraaa.Application.Features.Admin.Common
{
    /// <summary>A platform account as administrators see it.</summary>
    public record AdminUserResponse(
        Guid UserId,
        string FirstName,
        string LastName,
        string PhoneNumber,
        Role Role,
        bool IsDeactivated,
        DateTime? DeactivatedAtUtc,
        Guid? LibraryId,
        string? LibraryName,
        DateTime CreatedAt);
}
