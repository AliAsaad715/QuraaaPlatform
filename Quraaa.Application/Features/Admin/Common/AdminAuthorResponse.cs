namespace Quraaa.Application.Features.Admin.Common
{
    /// <summary>An author as administrators see it.</summary>
    public record AdminAuthorResponse(
        Guid AuthorId,
        string Name,
        string? Bio,
        string? PhotoUrl,
        DateTime? BirthDate,
        int BookCount,
        bool IsDeactivated,
        DateTime? DeactivatedAtUtc,
        DateTime CreatedAt);
}
